using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KH.CameraBehaviour;

namespace KH
{
	public class MainUICamera
	{
        public const string EVT_MainUICamera_MOVE = "EVT_MainUICamera_MOVE";

        static private MainUICamera _Instnace;
        static public MainUICamera getInstance()
        {
            if (_Instnace == null)
            {
                _Instnace = new MainUICamera();
            }
            return _Instnace;
        }

        private KHEventDispatcher _dispatcher;
        public KHEventDispatcher Dispatcher
        {
            get
            {
                if (_dispatcher == null)
                {
                    _dispatcher = new KHEventDispatcher(this);
                }
                return _dispatcher;
            }
        }

        public const string TouchSwitchChanged = "touchSwitchChanged";

        private List<KHCameraBehaviour> cameraBehaviourList;
        private UICamera uicamera;
        private Transform unityCamera;

        /// <summary>
        /// 当前的虚拟相机位置
        /// </summary>
        private Vector3 cameraPos;

        // 虚拟相机的目标位置
        private Vector3 destPos;
        public Vector3 DestPos
        {
            get { return destPos; }
        }

		private UIMainScene scene;
        private Vector2 positionLimitRectMax; // 镜头的 最大 可移动范围，对称的，[-x, x]
        private Rectangle positionLimitRectCurrent;// 镜头的 当前 可移动范围
        private PlayerController focus;

        private bool _touchSwitcher = true;
        public static bool isInNewUserGuide = false; // 是否正在新手引导
        public static bool isInPandoraPanel = false; // 潘多拉面板是否打开

        public UICamera CameraInstance
        {
            get
            {
                return this.uicamera;
            }
        }

        public bool TouchSwitcher
        {
            get
            {
                return _touchSwitcher
                    && KHUIManager.getInstance().IsWindowVisible(UIDef.NEW_USER_GUIDE_TIPS_VIEW) == false
                    && KHUIManager.getInstance().IsWindowVisible(UIDef.NEW_USER_GUIDE_MAIN_VIEW) == false
                    && UIUnlockSysPage.isShow == false
                    && MainUICamera.isInNewUserGuide == false
                    && FeatureUnlockCompleteTrigger.running == false
                    && MainUICamera.isInPandoraPanel == false;
            }

            set 
            {
                    if (_touchSwitcher != value)
                    {
                        _touchSwitcher = value;
                        Dispatcher.dispatchEvent(new KHEvent(TouchSwitchChanged)
                        {
                            data = _touchSwitcher
                        });
                        if (_touchSwitcher)
                        {
                            EasyTouch.SetEnabled(true);
                        }
                    }
            }
        }

		private Vector2 _positionPercent; // 镜头当前位置的百分比，[-1, 1]
		public Vector2 positionPercent
		{
			get {return _positionPercent;}
		}

        public float halfCameraWidth;

        public float speedRate = 0.023f;//0.005f * (1920f / Screen.width );
        public float destRate = 0.004f;
        private bool touchBegined = false;
        private float deltaSpeedX;
        private bool mNewMoveEvt = false;

        public float DeltaSpeedX
        {
            get { return deltaSpeedX; }
            set { deltaSpeedX = value; }
        }

        public bool IsmNewMoveEvt
        {
            get { return mNewMoveEvt; }
            set { mNewMoveEvt = value; }
        }

        private float startCameraPosX = 0.0f;
        private void On_SimpleTap(Gesture gesture)
        {
            if (this.TouchSwitcher)
                touchBegined = true;
            //Debuger.Log("00000000 On_SwipeStart ");
        }

        private void On_SwipeStart(Gesture gesture) 
        {
            if (!this.TouchSwitcher) { return; }
            startCameraPosX = destPos.x;
        }

        private void On_Swipe(Gesture gesture)
        {
            if (!this.TouchSwitcher) { return; }
            if (MessageManager.Instance.IsActivate && !MessageManager.Instance.IsSerializeToLocal) { return; }
            if (touchBegined)
            {
                deltaSpeedX = -gesture.deltaPosition.x;
                //float distanceX = deltaSpeedX * speedRate;
                float destX = (gesture.startPosition.x - gesture.position.x) * destRate + startCameraPosX;
                //MainUICamera.getInstance().move(distanceX, 0);
                //Debuger.Log("aaaa destX = " + destX + "  startCameraPosX = " + startCameraPosX + "  gesture.startPosition = " + gesture.startPosition + "   gesture.position = " + gesture.position);

                MainUICamera.getInstance().moveTo(destX, 0);

                if (MessageManager.Instance.IsActivate && MessageManager.Instance.IsSerializeToLocal)
                {
                    SwipeAction swipeAction = new SwipeAction(destX, deltaSpeedX, RemoteModel.Instance.CurrentTime);
                    MessageManager.Instance.serializeToLocal(swipeAction, MessageManager.DEST_PATH_DRAG_EVENT);
                }


                Debuger.Log("destX:" + destX);
                mNewMoveEvt = true;
            }
        }

        private void On_SwipeEnd(Gesture gesture)
        {
            //Debuger.Log("MCamera.On_SwipeEnd");
            touchBegined = false;
        }

        public static float InitCameraLookAtPos = 1.2f;

		public void initialize (UIMainScene scene)
		{
            if (Application.platform == RuntimePlatform.Android)
            {
                //安卓机型一测时手感速度提升
                speedRate = 0.064f;
            }
            destRate = 0.004f * (Screen.width / 960f);
			this.scene = scene;
            this.scene.dispatcher.addEventListener(KHEvent.COMPLETE, this.onSceneInit);
            Debuger.Log("test0");

            Debuger.Log("当前场景: " + Application.loadedLevel + " " + Application.loadedLevelName);

            //this.scene.dispatcher.addEventListener(KHSceneEvent.OPEN_AREA_CHANGE, this.onSceneInit);
            GameObject cameraObj = GameObject.Find("UI Root");
            Debuger.Log("test1");

            uicamera = cameraObj.transform.FindChild("Camera").GetComponent<UICamera>();
            Debuger.Log("test2");

            this.unityCamera = uicamera.transform;
            Debuger.Log("[初始化主场景相机] uicamera.name = " + uicamera.gameObject.name);
           
            cameraPos = new Vector3(InitCameraLookAtPos, 0, 0);//Vector3.zero;
            destPos = new Vector3(InitCameraLookAtPos, 0, 0); //Vector3.zero;
            this.cameraBehaviourList = new List<KHCameraBehaviour>();
            this.positionLimitRectCurrent = new Rectangle ();
            offset = Camera.main.orthographicSize * Camera.main.aspect * 0.3f;
            Debuger.Log("test3");

            RegisterGesture();
            calculatePositionLimitRect();
            calculatePositionPercent();

            //CreateBound();
		}

	    public void InitUICamera()
	    {
	        GameObject root = GameObject.Find("UI Root");
	        if (root == null) return;
	        Transform camera = root.transform.FindChild("Camera");
	        if (camera == null) return;
	        Debuger.Log("[sniperlin] InitUICamera()");
	        uicamera = camera.GetComponent<UICamera>();
	    }

        private void RegisterGesture()
        {
            Debuger.Log("++++++RegisterGesture");

            UnRegisterGesture();

            EasyTouch.On_TouchDown += On_SimpleTap;
            EasyTouch.On_SwipeStart += On_SwipeStart;
            EasyTouch.On_Swipe += On_Swipe;
            EasyTouch.On_SwipeEnd += On_SwipeEnd;
        }

        public void UnRegisterGesture()
        {
            Debuger.Log("------UnRegisterGesture");

            EasyTouch.On_TouchDown -= On_SimpleTap;
            EasyTouch.On_SwipeStart -= On_SwipeStart;
            EasyTouch.On_Swipe -= On_Swipe;
            EasyTouch.On_SwipeEnd -= On_SwipeEnd;
        }

        // 每次切换场景
		private void onSceneInit(KHEvent evt)
		{
            //UICamera.currentCamera.nearClipPlane = KHSceneUtil.CameraNear;
            //UICamera.currentCamera.farClipPlane = KHSceneUtil.CameraFar;
			this.calculatePositionLimitRect ();
		}

		// 移动镜头
		public void move (float dx, float dy)
		{
            if (this.scene != null && this.scene.gameObject != null)
			{
                Vector3 pos = cameraPos;// this.unityCamera.position;
                pos.z = 0;
				pos.x += dx;
				pos.y += dy;
                destPos = this.adjustPosition(pos);

				this.calculatePositionPercent();
			}
		}

        // 还是会带上拖拽的速度。
        public void moveTo(float dx, float dy)
        {
            if (scene.gameObject)
            {
                Vector3 pos = cameraPos;// this.unityCamera.position;
                pos.z = 0;
                pos.x = dx;
                pos.y = dy;
                destPos = this.adjustPosition(pos);

                this.calculatePositionPercent();
            }
        }

        // 忽略手指拖拽的速度。
        public void locateAt(float x, float y)
        {
            if (scene.gameObject)
            {
                this.deltaSpeedX = 0;
				Vector3 pos = cameraPos;
                pos.z = 0;
                pos.x = x;
                pos.y = y;
                destPos = this.adjustPosition(pos);

				/*
				 * 因为是一次性计算
				 * 需要立即收敛到目标位置
				 * by williamtyma
				 */
				cameraPos = destPos;
                this.calculatePositionPercent();
            }
        }

        //public void locateAt(Vector3 position, bool checkForAdjust=true)
        //{
        //    position.z = 0;
        //    if (checkForAdjust)
        //    {
        //        cameraPos = this.adjustPosition(position);
        //    }
        //    else
        //    {
        //        cameraPos = position;
        //    }
        //    this.calculatePositionPercent();
        //}
        public Vector3 getLookatPos()
        {
            return cameraPos;
        }

        public void bind(PlayerController focus)
        {
            this.focus = focus;
        }

        public void unbind()
        {
            this.focus = null;
            // TODO
        }

        public PlayerController GetFocus()
        {
            return focus;
        }

        private float offset;// = UICamera.currentCamera.orthographicSize * UICamera.currentCamera.aspect * 0.3f;
        public float Offset
        {
            get { return offset; }
            set { offset = value; }
        }
        private const float V_A = 0.13f;
        private const float TurnV_A = 0.06f;
        //private int lastDirection;
        //private Vector3 lastPos;
        public void update(float deltaTime)
        {
            //if (!touchSwitcher) { return; }
            if (!touchBegined && Mathf.Abs(deltaSpeedX) >= 0.001f)
            {
                float distanceX = deltaSpeedX * speedRate;
                MainUICamera.getInstance().move(distanceX, 0);
                deltaSpeedX -= deltaSpeedX * 0.06f;
                //Debuger.Log("MCamera.update deltaSpeedX = " + deltaSpeedX);
            }

            //cameraPos = (destPos - cameraPos) * 0.3f + cameraPos;
            this.RefreshCameraPosBy_DestPos();

			//Debuger.LogWarning("cameraPos.." + cameraPos.ToString());

            if (mNewMoveEvt && Mathf.Abs(destPos.x - cameraPos.x) < 0.01f)
            {
                mNewMoveEvt = false;
                Dispatcher.dispatchEvent(new KHEvent(EVT_MainUICamera_MOVE) { data = cameraPos });
            }

            //if (this.focus != null)
            //{
            //    Vector3 pos = this.focus.transform.position;
            //    int direction = this.focus.getMoveComponet().getDirection();
            //    pos.x += direction == Direction.LEFT ? -offset : offset;
            //    pos = this.adjustPosition(pos);
            //    float v = V_A;
            //    if (direction != lastDirection || Mathf.Abs(lastPos.x - pos.x) < 0.025)
            //        v = TurnV_A;
            //    lastPos = pos;
            //    pos.x = (pos.x - this.unityCamera.position.x) * v + this.unityCamera.position.x;
            //    pos.y = (pos.y - this.unityCamera.position.y) * v + this.unityCamera.position.y;
            //    this.locateAt(pos, false);
            //    lastDirection = direction;
            //}
            //List<KHCameraBehaviour> toRemove = new List<KHCameraBehaviour>();
            //foreach (KHCameraBehaviour behaviour in this.cameraBehaviourList)
            //{
            //    if (behaviour.update(deltaTime)) // 结束
            //    {
            //        toRemove.Add(behaviour);
            //    }
            //}
            //foreach (KHCameraBehaviour behaviour in toRemove)
            //{
            //    this.cameraBehaviourList.Remove(behaviour);
            //}
        }

        private void RefreshCameraPosBy_DestPos()
        {
            cameraPos = (destPos - cameraPos) * 0.3f + cameraPos;
        }

        //public void CheckOverSize()
        //{
        //    Vector3 pos = unityCamera.position;
        //    this.locateAt(cameraPos, true);
        //}

		// 纠正镜头
		private Vector3 adjustPosition(Vector3 pos)
		{
            if (pos.x < this.positionLimitRectCurrent.x)
			{
                pos.x = this.positionLimitRectCurrent.x;
			}
            else if (pos.x > this.positionLimitRectCurrent.right)
			{
                pos.x = this.positionLimitRectCurrent.right;
			}

            if (pos.y > this.positionLimitRectCurrent.y)
			{
                pos.y = this.positionLimitRectCurrent.y;
			}
            else if (pos.y < this.positionLimitRectCurrent.bottom)
			{
                pos.y = this.positionLimitRectCurrent.bottom;
			}
            pos.z = 0;

            //Debuger.LogWarning(string.Format("adjustPosition {0}", pos.ToString()));

			return pos;
		}

		// 计算镜头 最大和当前的 可移动范围
		private void calculatePositionLimitRect()
		{
            Camera camera = Camera.main;
            this.halfCameraWidth = camera.orthographicSize * camera.aspect;
            KHSceneInfo sceneInfo = this.scene.sceneInfo;
            float halfSceneWidth = sceneInfo.width / 2;
            float halfSceneHeight = sceneInfo.height / 2;
            this.positionLimitRectMax.x = halfSceneWidth - this.halfCameraWidth;
            this.positionLimitRectMax.y = halfSceneHeight - camera.orthographicSize;
            this.positionLimitRectMax.x = this.positionLimitRectMax.x < 0 ? 0 : this.positionLimitRectMax.x;
            this.positionLimitRectMax.y = this.positionLimitRectMax.y < 0 ? 0 : this.positionLimitRectMax.y;

            this.positionLimitRectCurrent.x = -this.positionLimitRectMax.x;
            this.positionLimitRectCurrent.width = Mathf.Abs(this.positionLimitRectMax.x*2);
            this.positionLimitRectCurrent.y = this.positionLimitRectMax.y;
            this.positionLimitRectCurrent.height = Mathf.Abs(this.positionLimitRectMax.y*2);
		}

		// 计算当前位置的百分比
		private void calculatePositionPercent()
		{
            //Vector3 pos = this.unityCamera.position;
            this._positionPercent.x = this.positionLimitRectMax.x == 0 ? 0 : (cameraPos.x / this.positionLimitRectMax.x);
            this._positionPercent.y = this.positionLimitRectMax.y == 0 ? 0 : (cameraPos.y / this.positionLimitRectMax.y);

            //Debuger.LogWarning(string.Format("calculatePositionPercent {0},{1}", this._positionPercent.x, this._positionPercent.y));
		}

        public Vector3 position
        {
            get { return cameraPos; }
            set { cameraPos = value; }
        }

        public void UnBuild()
        {
            InitCameraLookAtPos = DestPos.x;
        }

        //public void addBehaviour(KHCameraBehaviour value)
        //{
        //    if (value.only)
        //    {
        //        foreach (KHCameraBehaviour behaviour in this.cameraBehaviourList)
        //        {
        //            if (behaviour.type == value.type)
        //            {
        //                behaviour.OnDisturb();
        //                this.cameraBehaviourList.Remove(behaviour);
        //                break;
        //            }
        //        }
        //    }
        //    this.cameraBehaviourList.Add(value);
        //}

        //public KHCameraBehaviour GetBehavior(string _key)
        //{
        //    for (int i = 0; i < cameraBehaviourList.Count; ++i)
        //    {
        //        if (cameraBehaviourList[i].type == _key)
        //        {
        //            return cameraBehaviourList[i];
        //        }
        //    }
        //    return null;
        //}

        //public void RemoveBehavior(string _key, bool disturb = true)
        //{
        //    foreach (KHCameraBehaviour behaviour in this.cameraBehaviourList)
        //    {
        //        if (behaviour.type == _key)
        //        {
        //            if(disturb)
        //                behaviour.OnDisturb();
        //            this.cameraBehaviourList.Remove(behaviour);
        //            break;
        //        }
        //    }
        //}


        //public void SetCameraSize(float _size)
        //{
        //    Camera.main.orthographicSize = _baseCameraScale * _size;
        //    calculatePositionLimitRect();
        //}

        //public void SetPosImmediately(Vector3 _pos)
        //{
        //    _pos.z = 0;
        //    this.unityCamera.position = _pos;
        //}

        //public Vector3 GetCameraPosBySize(float _size, Vector3 _target, int _dir)
        //{
        //    _target.x += (float)_dir * offset;
        //    float tmp = Camera.main.orthographicSize;
        //    Camera.main.orthographicSize = _baseCameraScale * _size;
        //    calculatePositionLimitRect();
        //    Camera.main.orthographicSize = tmp;
        //    return this.adjustPosition(_target);
        //}
	}
}