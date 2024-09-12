using UnityEngine;
using UnityEngine.Events;
using System;
using Cysharp.Threading.Tasks;
using Cinemachine;

namespace LDtkLevelManager.Implementations.Basic
{
    public class LevelTransitioner : MonoBehaviour
    {
        #region Static

        private static LevelTransitioner _instance;
        public static LevelTransitioner Instance => _instance;

        #endregion

        #region Inspector        

        [SerializeField]
        private Canvas _curtainsCanvas;

        [SerializeField]
        private Animator _curtainsPrefab;

        [SerializeField]
        private UnityEvent _transitionStartedEvent;

        [SerializeField]
        private UnityEvent _transitionEndedEvent;

        #endregion

        #region Fields

        private bool _transitioning = false;
        private Animator _curtainsAnimator;

        #endregion

        #region Getters

        /// <summary>
        /// Whether the <see cref="LevelTransitioner"/> is currently transitioning.
        /// </summary>
        public bool Transitioning => _transitioning;

        /// <summary>
        /// The event fired when the transition begins.
        /// </summary>
        public UnityEvent TransitionStartedEvent => _transitionStartedEvent;

        /// <summary>
        /// The event fired when the transition ends.
        /// </summary>
        public UnityEvent TransitionEndedEvent => _transitionEndedEvent;

        #endregion

        #region Behaviour

        private void Awake()
        {
            _instance = this;
            _curtainsAnimator = Instantiate(_curtainsPrefab, _curtainsCanvas.transform);
        }

        private void OnDestroy()
        {
            _instance = null;
        }


        #endregion

        #region Transition performing

        /// <summary>
        /// Loads a level, prepares it for transition, then transitions into it
        /// using the given spot.
        /// </summary>
        /// <param name="levelIid">The LDtk Iid of the level.</param>
        /// <param name="spotIid">The LDtk Iid of the spot in the level.</param>
        public void TransitionToSpot(ILevelFlowSubject subject, string levelIid, string spotIid)
        {
            _ = TransitionToLevelAsync(subject, levelIid, spotIid);
        }

        /// <summary>
        /// Loads a level, prepares it for transition, then transitions into it
        /// using the given connection.
        /// </summary>
        /// <param name="levelIid">The LDtk Iid of the level.</param>
        /// <param name="connection">The connection to transition from.</param>
        public void TransitionToConnection(ILevelFlowSubject subject, string levelIid, IConnection connection)
        {
            _ = TransitionToLevelAsync(subject, levelIid, connection);
        }

        /// <summary>
        /// Loads a level, prepares it for transition, then transitions into it
        /// using the given portal.
        /// </summary>
        /// <param name="levelIid">The LDtk Iid of the level.</param>
        /// <param name="portal">The portal to transition to.</param>
        public void TransitionToPortal(ILevelFlowSubject subject, string levelIid, IPortal portal)
        {
            _ = TransitionToPortalAsync(subject, levelIid, portal);
        }

        /// <summary>
        /// Loads a level, prepares it for transition, then transitions into it.
        /// </summary>
        /// <param name="levelIid">The LDtk Iid of the level.</param>
        /// <param name="spotIid">The Iid of the spot to transition to.</param>
        /// <returns>A UniTask that completes when the transition is complete.</returns>
        public async UniTask TransitionToLevelAsync(ILevelFlowSubject subject, string levelIid, string spotIid)
        {
            // Exit the current level
            LevelLoader.Instance.DeactivatePreparedLevel();

            // Run the task that should be run before the level is prepared
            await BeforePreparationTask();

            // Get the current camera
            CinemachineVirtualCamera camera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera as CinemachineVirtualCamera;

            // Load the new level
            await LevelLoader.Instance.LoadUniverseLevel(levelIid);

            // If there was a camera, make it inactive
            if (camera != null)
            {
                camera.Follow = null;
                camera.gameObject.SetActive(false);
            }

            // Prepare the new level for transition
            LevelLoader.Instance.Prepare(subject, levelIid, spotIid);

            // Run the task that should be run after the level is prepared
            await AfterPreparationTask();
        }

        /// <summary>
        /// Transitions to a new level and prepares it for entrance,
        /// then opens the curtains and enters the level.
        /// </summary>
        /// <param name="levelIid">The LDtk Iid of the level to transition to.</param>
        /// <param name="connection">The connection to transition from.</param>
        /// <returns>A UniTask that completes when the transition is complete.</returns>
        public async UniTask TransitionToLevelAsync(ILevelFlowSubject subject, string levelIid, IConnection connection)
        {
            // Exit the current level
            LevelLoader.Instance.DeactivatePreparedLevel();

            // Run the task that should be run before the level is prepared
            await BeforePreparationTask();

            // Get the current camera
            CinemachineVirtualCamera camera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera as CinemachineVirtualCamera;

            // Load the new level
            await LevelLoader.Instance.LoadUniverseLevel(levelIid);

            // If there was a camera, make it inactive
            if (camera != null)
            {
                camera.Follow = null;
                camera.gameObject.SetActive(false);
            }

            // Prepare the new level for transition
            LevelLoader.Instance.Prepare(subject, levelIid, connection);

            // Run the task that should be run after the level is prepared
            await AfterPreparationTask();
        }

        /// <summary>
        /// Transitions the player to a new level and prepares it for entrance,
        /// then opens the curtains and enters the level.
        /// </summary>
        /// <param name="levelIid">The LDtk Iid of the level to transition to.</param>
        /// <param name="portal">The portal to transition from.</param>
        /// <returns>A UniTask that completes when the transition is complete.</returns>
        public async UniTask TransitionToPortalAsync(ILevelFlowSubject subject, string levelIid, IPortal portal)
        {
            if (!LevelLoader.Instance.TryGetLevel(levelIid, out LevelInfo level))
            {
                Logger.Error($"Level under LDtk Iid {levelIid} not present in project {LevelLoader.Instance.Project.name}", this);
                return;
            }

            // Notify the outside world that the transition has started
            _transitioning = true;
            _transitionStartedEvent.Invoke();

            // Exit the current level, close the curtains, prepare the new level,
            // then enter it.
            LevelLoader.Instance.DeactivatePreparedLevel();
            await CloseCurtains();

            // In this example we unload all stand alone levels upon using portals. But
            // you can choose to keep them loaded if you want.
            await LevelLoader.Instance.UnloadAllStandaloneLevels();

            if (level.StandAlone)
            {
                await LevelLoader.Instance.LoadStandaloneLevel(level);
            }
            else
            {
                await LevelLoader.Instance.LoadUniverseLevel(level);
            }

            LevelLoader.Instance.Prepare(subject, levelIid, portal);
            await WaitOnCameraBlend();
            await OpenCurtains();
            LevelLoader.Instance.ActivatePreparedLevel();

            // Notify the outside world that the transition has ended
            _transitioning = false;
            _transitionEndedEvent.Invoke();
        }

        /// <summary>
        /// This is called before loading a new level. It fires the "transition started" event,
        /// then closes the curtains, and then exits the current level.
        /// </summary>
        /// <returns>A UniTask that completes after the transition has started.</returns>
        private async UniTask BeforePreparationTask()
        {
            _transitioning = true;
            _transitionStartedEvent.Invoke();

            await PerformTransitions(LevelTransitionMoment.Close);

            LevelLoader.Instance.DeactivatePreparedLevel();
        }

        /// <summary>
        /// Waits for the curtains to open and the camera blend to finish,
        /// then activates the level and notifies that the transition has ended.
        /// </summary>
        private async UniTask AfterPreparationTask()
        {
            // Wait on transitions
            await PerformTransitions(LevelTransitionMoment.Open);
            await WaitOnCameraBlend();

            // "Activating" level
            LevelLoader.Instance.ActivatePreparedLevel();

            _transitioning = false;
            _transitionEndedEvent.Invoke();
        }

        /// <summary>
        /// Performs the transitions for the given <paramref name="moment"/>.
        /// This is where you put your transition logic, like animations, sound effects, etc.
        /// </summary>
        /// <param name="moment">The moment to transition to.</param>
        /// <returns>A <see cref="UniTask"/> that completes when the transition is over.</returns>
        private async UniTask PerformTransitions(LevelTransitionMoment moment)
        {
            // For now this is just a dummy transition
            await UniTask.CompletedTask;
        }

        #endregion

        /// <summary>
        /// Waits until the camera blend is over.
        /// </summary>
        /// <param name="brain">The <see cref="CinemachineBrain"/> to wait for.</param>
        /// <returns>A <see cref="UniTask"/> that completes when the camera blend is over.</returns>
        private async UniTask WaitOnCameraBlend()
        {
            CinemachineBrain brain = CinemachineCore.Instance.GetActiveBrain(0);

            if (brain == null) return;

            // Delays for 0.3 seconds. Enough time for the camera to initiate
            // any possible blending.
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f));

            // Get the active blend
            var blend = brain.ActiveBlend;

            // If there's no active blend, return
            if (blend == null) return;

            // Calculate the delay until the blend is over
            float delay = blend.Duration > 0.3f ? blend.Duration - 0.3f : 0f;

            // Wait until the blend is over
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
        }

        /// <summary>
        /// Closes the curtains (displays loading screen)
        /// </summary>
        /// <returns></returns>
        public async UniTask CloseCurtains()
        {
            // Play the animation
            _curtainsAnimator.Play("CurtainsClose");

            // Wait for the animation to finish
            int length = _curtainsAnimator.GetCurrentAnimatorClipInfo(0).Length;
            await UniTask.Delay(TimeSpan.FromSeconds(length));
        }

        /// <summary>
        /// Opens the curtains (hides loading screen)
        /// </summary>
        /// <returns></returns>
        public async UniTask OpenCurtains()
        {
            // Play the animation
            _curtainsAnimator.Play("CurtainsOpen");

            // Wait for the animation to finish
            int length = _curtainsAnimator.GetCurrentAnimatorClipInfo(0).Length;
            await UniTask.Delay(TimeSpan.FromSeconds(length));

            // If there is a CinemachineBrain attached to the camera, wait for the camera to finish its blend
            if (Camera.main.TryGetComponent<CinemachineBrain>(out var cinemachineBrain))
            {
                await WaitOnCameraBlend();
            }
        }
    }
}