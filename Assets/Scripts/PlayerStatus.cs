﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using XboxCtrlrInput;
using XInputDotNetPure;
public class PlayerStatus : MonoBehaviour, IHitByMelee
{
    private float m_iMaxHealth;
    public float m_iHealth = 3; //health completely useless right now
    int m_iTimesPunched = 0;
    int m_iPreviousTimesPunched = 0;
    bool m_bDead = false;
    public bool m_bStunned = false;
    public bool m_bMiniStun;
    public float StunedSlide = 400;
    public int m_iScore;
    public bool m_bInvincible = false;
    public float m_fInvincibleTime = 3.0f;
    public float test = 0.5f;
    private Timer m_InvincibilityTimer;
    public bool IsDead { get { return m_bDead; } set { m_bDead = value; } }
    public bool IsStunned { get { return m_bStunned; } set { m_bStunned = value; } }
    public int TimesPunched { get { return m_iTimesPunched; } set { m_iTimesPunched = value; } }

    public float m_fStunTime = 1;
    public float m_fMaximumStunWait = 2;
    public float m_fStunTimerReduction = 0.5f;
    Timer stunTimer;
    Timer resetStunTimer;

    [HideInInspector]
    public Color _playerColor;
    private Renderer PlayerSprite;

    private BaseAbility m_Ability;

    public GameObject killMePrompt = null;
    public GameObject killMeArea = null;

    private GameObject _PlayerCanvas;
    private GameObject _HealthMask;
    private GameObject HealthContainer;
    [SerializeField]
    private Image HealthLost;
    private Timer healthLossTimer;
    private Timer ShowHealthChangeTimer; 
    private bool m_bShowHealthLoss = false;
    private bool m_bShowHealthChange = false;
    Rigidbody2D _rigidbody;
    [HideInInspector]
    public int spawnIndex;

    public Image stunBar;
    public Image stunMask;
    private GameObject stunBarContainer;
    private AudioSource m_MeleeHitAudioSource;

    public Sprite[] DeadSprites;
    public Sprite[] StunnedSprites;
    private bool DeathSpriteChanged = false;
    private bool StunSpriteChanged = false;
    private SpriteRenderer m_SpriteRenderer;
    private CameraControl _cameraControlInstance;
    [Range(0, 0.22f)]
    public float fill;
    //if the player is dead, the renderer will change their Color to gray, and all physics simulation of the player's rigidbody will be turned off.
    void Start()
    {
        ShowHealthChangeTimer = new Timer(1.5f);
        healthLossTimer = new Timer(0.9f);

        _cameraControlInstance = CameraControl.mInstance;
        m_SpriteRenderer = this.transform.Find("Sprites").GetChild(0).GetComponent<SpriteRenderer>();

        m_MeleeHitAudioSource = this.gameObject.AddComponent<AudioSource>();
        m_MeleeHitAudioSource.outputAudioMixerGroup = (Resources.Load("AudioMixer/SFXAudio") as GameObject).GetComponent<AudioSource>().outputAudioMixerGroup;
        //m_MeleeHitAudioSource.outputAudioMixerGroup = (Resources.Load("AudioMixer/SFXAudio") as  AudioSource).outputAudioMixerGroup;
        m_MeleeHitAudioSource.playOnAwake = false;
        m_MeleeHitAudioSource.spatialBlend = 1;
        m_Ability = this.GetComponent<BaseAbility>();
        m_InvincibilityTimer = new Timer(m_fInvincibleTime);
        _rigidbody = GetComponent<Rigidbody2D>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        m_iMaxHealth = m_iHealth;
        //initialize my timer and get the player's Color to return to.
        stunTimer = new Timer(m_fStunTime);
        resetStunTimer = new Timer(m_fMaximumStunWait);
        //_playerColor = GetComponent<Renderer>().material.color;

        if (GetComponent<Renderer>())
        {
            PlayerSprite = GetComponent<Renderer>();
            _playerColor = GetComponent<Renderer>().material.color;
        }
        else
        {
            PlayerSprite = transform.Find("Sprites").Find("PlayerSprite").GetComponent<Renderer>();
            _playerColor = transform.Find("Sprites").Find("PlayerSprite").GetComponent<Renderer>().material.color;
        }
        killMePrompt.SetActive(false);

        LoadUIBars();
        //foreach (var item in PlayerUIArray.Instance.playerElements[GetComponent<ControllerSetter>().m_playerNumber].m_objects)
        //{
        //    item.SetActive(true);
        //}
    }

    void Awake()
    {
        m_bInvincible = true;
    }
    void Update()
    {
        if (stunBarContainer.activeSelf)
        {
            float sunOffset = stunTimer.CurrentTime / stunTimer.mfTimeToWait * 0.24f;
            stunMask.material.SetTextureOffset("_MainTex", new Vector2(-0.24f + sunOffset, 0));
        }

        if (m_iHealth <= 0)
            m_iHealth = 0;

        StartCoroutine(InvinciblityTime());
        //update my score
        if (GameManagerc.Instance.PlayerWins.ContainsKey(this))
        {
            m_iScore = GameManagerc.Instance.PlayerWins[this];
        }

        //if i've been punched once, start the timer, once the timer has reached the end, reset the amount of times punched.
        if (m_iTimesPunched >= 1)
        {
            if (m_iTimesPunched != m_iPreviousTimesPunched)
            {
                resetStunTimer.SetTimer(0);
                m_iPreviousTimesPunched = m_iTimesPunched;
            }
            if (resetStunTimer.Tick(Time.deltaTime))
            {
                //  m_iTimesPunched = 0;
                // m_iPreviousTimesPunched = 0; 
                //TODO Needs to be readded at some point.
            }
        }

        if (_HealthMask)
        {
            float xOffset = m_iHealth * -0.0791f;
            _HealthMask.GetComponent<Image>().material.SetTextureOffset("_MainTex", new Vector2(0 + xOffset, 0));

            if (m_bShowHealthLoss)
            {
                if (healthLossTimer.Tick(Time.deltaTime))
                {
                    HealthLost.fillAmount = m_iHealth / 3;
                    m_bShowHealthLoss = false;
                }
            }
        }

        //if im dead, set my Color to gray, turn of all physics simulations and exit the function
        if (m_bDead)
        {
            SetAllAnimatorsFalse();

            PlayerSprite.material.color = Color.grey;
            this.GetComponent<Rigidbody2D>().simulated = false;
            killMePrompt.SetActive(false);
            killMeArea.SetActive(false);
            stunBarContainer.SetActive(false);
            GetComponent<Move>().GetBodyAnimator().enabled = false;
            GetComponent<Move>().GetFeetAnimator().enabled = false;
            if (DeadSprites.Length > 0 && !DeathSpriteChanged)
            {
                DeathSpriteChanged = true;
                m_SpriteRenderer.sprite = DeadSprites[Random.Range(0, DeadSprites.Length - 1)];
            }

            return;
        }

        //if im stunned, make me cyan and show any kill prompts (X button and kill radius);
        if (m_bStunned)
        {
            stunTimer.mfTimeToWait = m_fStunTime;
            GetComponent<Move>().GetBodyAnimator().enabled = false;
            GetComponent<Move>().GetFeetAnimator().enabled = false;
            m_Ability.m_ChargeIndicator.SetActive(false);
            m_Ability.ChargeCoolDown = false;
            if (StunnedSprites.Length > 0 && !StunSpriteChanged)
            {
                StunSpriteChanged = true;
                m_SpriteRenderer.sprite = StunnedSprites[Random.Range(0, StunnedSprites.Length - 1)];
            }

            SetAllAnimatorsFalse();
            killMeArea.SetActive(true);
            PlayerSprite.material.color = Color.cyan;
            //set the stun bar location
            //stunBarContainer.transform.localPosition = Vector3.zero;
            //stunBarContainer.transform.position += Vector3.up * 1.2f;
            //stunBar.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            //stunMask.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            //stunBarContainer.SetActive(true);
            stunBarContainer.SetActive(true);

            CheckForButtonMash();
            //this.GetComponent<Renderer>().material.color = Color.cyan;
            if (this.transform.GetChild(1).tag == "Stunned")
            {
                this.transform.GetChild(1).gameObject.GetComponent<Collider2D>().enabled = true;
            }
            else
            {
                this.transform.GetChild(0).gameObject.GetComponent<Collider2D>().enabled = true;
            }
            this.GetComponent<Move>().MakeCollidersTriggers(true);
            if (stunTimer.Tick(Time.deltaTime))
            {
                m_bStunned = false;
            }
        }
        //if not stunned dont kill me
        else
        {
            GetComponent<Move>().GetBodyAnimator().enabled = true;
            GetComponent<Move>().GetFeetAnimator().enabled = true;
            m_Ability.m_ChargeIndicator.SetActive(true);
            m_Ability.ChargeCoolDown = true;
            StunSpriteChanged = false;
            this.GetComponent<Move>().MakeCollidersTriggers(false);
            stunBarContainer.SetActive(false);
            if (this.transform.GetChild(1).tag == "Stunned")
            {
                Debug.Log(this.transform.GetChild(0).tag);
                this.transform.GetChild(1).gameObject.GetComponent<Collider2D>().enabled = false;        //? child 0 is weaponSpot... 
            }
            else
            {
                this.transform.GetChild(0).gameObject.GetComponent<Collider2D>().enabled = false;
            }
            killMeArea.SetActive(false);
            killMePrompt.SetActive(false);
            //If I find a regular renderer
            if (GetComponent<Renderer>())
            {
                GetComponent<Renderer>().material.color = _playerColor;
            }
            else //if no rendere was found
            {
                if (!m_bInvincible)
                    PlayerSprite.GetComponent<Renderer>().material.color = _playerColor;
            }
        }

    }
    public void LateUpdate()
    {
        //If the previous frames health isnt the current frames health, show the changed health.
        _PlayerCanvas.transform.localScale = new Vector3(Camera.main.orthographicSize, Camera.main.orthographicSize, Camera.main.orthographicSize) * 0.003f;
        //_PlayerCanvas.transform.position = this.transform.position + Vector3.up;
        HealthContainer.transform.position = this.transform.position + Vector3.up;
        stunBarContainer.transform.position = this.transform.position + Vector3.up;

        if (m_bShowHealthChange)
        {
            HealthContainer.SetActive(true);
            //HealthContainer.transform.position = -this.transform.up * 0.5f;

            if (ShowHealthChangeTimer.Tick(Time.deltaTime))
            {
                HealthContainer.SetActive(false);
                m_bShowHealthChange = false;
            }
        }
    }


    public void MiniStun(Vector3 ForceApplied, float StunTime)
    {
        m_bMiniStun = true;
        //TODO change to attacking animators = false maybe.
        //SetAllAnimatorsFalse();
        //        GetComponent<Move>().SetActive(false);
        _rigidbody.velocity = ForceApplied;
        Debug.Log("Corotuine should be here");
        StartCoroutine(MiniStun(StunTime));

    }

    public IEnumerator MiniStun(float StunTime)
    {
        //Debug.Log(StunTime);
        yield return new WaitForSeconds(StunTime);
        m_bMiniStun = false;
        yield return null;
        //Debug.Log("Done");
    }

    /// <summary>
    /// Used for combining a stun effect with a knock back. If no stun required use "Knockback()"
    /// </summary>
    /// <param name="ThrownItemVelocity"></param>

    public void StunPlayer(Vector3 ThrownItemVelocity)
    {
        //stun the player called outside of class
        //Vector3 a = ThrownItemVelocity.normalized;
        // _rigidbody.velocity = (a * StunedSlide);
        SetAllAnimatorsFalse();
        _rigidbody.velocity = ThrownItemVelocity;
        m_bStunned = true;
        m_iTimesPunched = 0;

    }
    /// <summary>
    /// Used for knocking a player back without stunning them.
    /// </summary>
    /// <param name="KnockBackVelocity"></param>
    public void KnockBack(Vector3 KnockBackVelocity)
    {
        _rigidbody.velocity = KnockBackVelocity;
    }


    public void ResetPlayer()
    {
        m_iHealth = 3;
        m_bDead = false;
        m_bStunned = false;
        m_iTimesPunched = 0;
        stunTimer.CurrentTime = 0;
        this.GetComponent<Rigidbody2D>().simulated = true;
        m_Ability.m_iMaxCharges = 0;
        float xOffset = m_iHealth * -0.0791f;
        _HealthMask.GetComponent<Image>().material.SetTextureOffset("_MainTex", new Vector2(0 + xOffset, 0));
        this.transform.position = ControllerManager.Instance.spawnPoints[spawnIndex].position;
        //this.transform.position = Vector3.zero;
        GetComponent<Move>().ThrowWeapon(Vector2.zero, Vector2.up, false);
        _PlayerCanvas.transform.SetParent(this.transform.Find("Sprites"));
        // this.GetComponent<Collider2D>().isTrigger = true;
        m_bInvincible = true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Load UI stuff.
        LoadUIBars();

        if (scene.buildIndex == 0)
        {
            Destroy(this.gameObject);
            return;
        }
        //time to re activate all the UI stuff
        this.GetComponent<BaseAbility>().GetUIElements();

        //foreach (var item in PlayerUIArray.Instance.playerElements[GetComponent<ControllerSetter>().m_playerNumber].m_objects)
        //{
        //    item.SetActive(true);
        //}
    }
    public void KillPlayer(PlayerStatus killer)
    {
        //kill the player, called outside of class (mostly used for downed kills)
        if (/*!m_bInvincible*/true)
        {
            SetAllAnimatorsFalse();
            m_iHealth = 0;
            m_bDead = true;
            //if (GameManagerc.Instance.m_gameMode == Gamemode_type.DEATHMATCH_POINTS)
            //    GameManagerc.Instance.PlayerWins[killer]++;
        }

    }

    public void HitPlayer(Bullet aBullet, bool abGiveIFrames = false)
    {
        if (!m_bInvincible)
        {
            healthLossTimer.CurrentTime = 0;
            m_bShowHealthLoss = true;
            ShowHealthChangeTimer.CurrentTime = 0;
            m_bShowHealthChange = true;
            m_iHealth -= aBullet.m_iDamage;
            //If the game mode is either the timed deathmatch or scores appointed on kills deathmatch, then give them points
            if (m_iHealth <= 0 /*&& (GameManagerc.Instance.m_gameMode == Gamemode_type.DEATHMATCH_POINTS *//*|| GameManagerc.Instance.m_gameMode == Gamemode_type.DEATHMATCH_TIMED*/)
            {
                //update the bullet owner's score
                GameManagerc.Instance.PlayerWins[aBullet.bulletOwner]++;
            }
        }
        if (abGiveIFrames)
        {
            m_bInvincible = true;
        }
    }
    public void HitPlayer(Weapon a_weapon, bool abGiveIFrames = false)
    {
        if (!m_bInvincible)
        {
            healthLossTimer.CurrentTime = 0;
            m_bShowHealthLoss = true;
            ShowHealthChangeTimer.CurrentTime = 0;
            m_bShowHealthChange = true;
            m_iHealth -= a_weapon.m_iDamage;
            //If the game mode is either the timed deathmatch or scores appointed on kills deathmatch, then give them points
            if (m_iHealth <= 0 /*&& (GameManagerc.Instance.m_gameMode == Gamemode_type.DEATHMATCH_POINTS*/ /*|| GameManagerc.Instance.m_gameMode == Gamemode_type.DEATHMATCH_TIMED*/)
            {
                //update the bullet owner's score
                GameManagerc.Instance.PlayerWins[a_weapon.transform.root.GetComponent<PlayerStatus>()]++;
            }
        }
        if (abGiveIFrames)
        {
            m_bInvincible = true;
        }
    }

    IEnumerator InvinciblityTime()
    {
        if (m_InvincibilityTimer.mfTimeToWait != m_fInvincibleTime)
        {
            m_InvincibilityTimer = new Timer(m_fInvincibleTime);
        }

        if (m_bInvincible)
        {
            if (m_InvincibilityTimer.Tick(Time.deltaTime))
            {
                m_bInvincible = false;
            }
            Material m = PlayerSprite.GetComponent<Renderer>().material;

            PlayerSprite.GetComponent<Renderer>().material = null;
            PlayerSprite.GetComponent<Renderer>().material.color = Color.white;
            yield return new WaitForSecondsRealtime(test);
            PlayerSprite.GetComponent<Renderer>().material = m;
            PlayerSprite.GetComponent<Renderer>().material.color = _playerColor;
        }

        yield return null;
    }

    public void Clear()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void CheckForButtonMash()
    {
        if (!m_bMiniStun)
        {
            if (XCI.GetButtonDown(XboxButton.X, GetComponent<ControllerSetter>().mXboxController))
            {
                stunTimer.CurrentTime += m_fStunTimerReduction;
            }
        }

    }

    void SetAllAnimatorsFalse()
    {
        Animator Body = GetComponent<Move>().GetBodyAnimator();
        Animator Feet = GetComponent<Move>().GetFeetAnimator();

        //If the body animator is found, go through each parameter, and set it to false
        if (Body)
        {
            foreach (AnimatorControllerParameter parameter in Body.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool)
                    Body.SetBool(parameter.name, false);
            }
        }
        //same with feet
        if (Feet)
        {
            foreach (AnimatorControllerParameter parameter in Feet.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool)
                    Feet.SetBool(parameter.name, false);
            }
        }
    }

    public void HitByMelee(Weapon meleeWeapon, AudioClip soundEffect, float Volume = 1, float Pitch = 1)
    {
        m_MeleeHitAudioSource.clip = soundEffect;
        m_MeleeHitAudioSource.volume = Volume;
        m_MeleeHitAudioSource.pitch = Pitch;
        m_MeleeHitAudioSource.Play();
        //GetComponent<AudioSource>().PlayOneShot(soundEffect , Volume); 
    }


    void LoadUIBars()
    {

        //Player bars and shit
        stunBarContainer = this.transform.Find("Sprites").Find("PlayerCanvas").GetChild(1).gameObject;
        stunMask = stunBarContainer.transform.GetChild(0).GetComponent<Image>();

        _HealthMask = this.transform.Find("Sprites").Find("PlayerCanvas").GetChild(0).GetChild(0).gameObject;
        _PlayerCanvas = this.transform.Find("Sprites").Find("PlayerCanvas").gameObject;
        Material temp = new Material(_HealthMask.GetComponent<Image>().material.shader);
        _HealthMask.GetComponent<Image>().material = temp;
        HealthContainer = this.transform.Find("Sprites").Find("PlayerCanvas").GetChild(0).gameObject;
        HealthLost = this.transform.Find("Sprites").Find("PlayerCanvas").GetChild(0).GetChild(1).GetComponent<Image>();

        //Set health bar colours
        foreach (var item in HealthContainer.GetComponentsInChildren<Image>())
        {
            Material oldMat = item.GetComponent<Image>().material;

            Material tempMaterial = new Material(item.GetComponent<Image>().material.shader);
            item.GetComponent<Image>().material = tempMaterial;
            if (item.GetComponent<Image>().material.HasProperty("_Color"))
                item.GetComponent<Image>().material.color = _playerColor;
        }
        //Set the stun bar container colours
        foreach (var item in stunBarContainer.GetComponentsInChildren<Image>())
        {
            Material oldMat = item.GetComponent<Image>().material;

            Material tempMaterial = new Material(item.GetComponent<Image>().material.shader);
            item.GetComponent<Image>().material = tempMaterial;
            if (item.GetComponent<Image>().material.HasProperty("_Color"))
                item.GetComponent<Image>().material.color = _playerColor;
        }
        HealthLost.color = Colors.Yellow;
        _PlayerCanvas.transform.SetParent(null);
        HealthContainer.SetActive(false);

    }
}







