using System.Collections;
using Kamgam.UIToolkitTextAnimation.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public class ScriptingDemo : MonoBehaviour
    {
        protected UIDocument _doc;

        public UIDocument Doc
        {
            get
            {
                if (_doc == null)
                {
                    _doc = this.GetComponent<UIDocument>();
                }

                return _doc;
            }
        }

        private TextAnimationManipulator m_WaveManipulator;
        private TextAnimationManipulator m_SwingManipulator;

        void Awake()
        {
            // If you use Awake then you have to use TextAnimationDocument.RegisterOnEnable() because
            // the manipulators may not have been added yet (depending on the "random" initialization
            // order. My recommendation: Do not use Awake for this.
        }

        // If you use Start instead on OnEnable() then you do not have to use 
        // TextAnimationDocument.RegisterOnEnable() but can access the manipulators directly.
        void Start()
        {
            var root = Doc.rootVisualElement;
            
            // Fetch the manipulators of the two labels. There is always only one TextAnimationManipulator
            // per TextElement no matter how many animations (typewriter or char) are used.
            
            var waveLabel = root.Q(name: "TypewriterWave");
            m_WaveManipulator = TextAnimationManipulator.GetManipulator(waveLabel);
            
            var swingLabel = root.Q(name: "TypewriterSwingIn");
            m_SwingManipulator = swingLabel.TAGetManipulator(); // <- using extension methods

            
            // Events to restart all animations
            
            var restartBtn = root.Q<Button>("RestartButton");
            restartBtn.RegisterCallback<ClickEvent>(onRestartAll, TrickleDown.TrickleDown);
            
            var pauseBtn = root.Q<Button>("PauseButton");
            pauseBtn.RegisterCallback<ClickEvent>(onPauseAll, TrickleDown.TrickleDown);
            
            var playBtn = root.Q<Button>("PlayButton");
            playBtn.RegisterCallback<ClickEvent>(onPlayAll, TrickleDown.TrickleDown);
            
            // Events to control only the rainbow character animation in the first label.
            
            var restartRainbowBtn = root.Q<Button>("RestartAnimationButton");
            restartRainbowBtn.RegisterCallback<ClickEvent>(onRestartAnimation, TrickleDown.TrickleDown);
            
            var pauseRainbowBtn = root.Q<Button>("PauseAnimationButton");
            pauseRainbowBtn.RegisterCallback<ClickEvent>(onPauseAnimation, TrickleDown.TrickleDown);
            
            var playRainbowBtn = root.Q<Button>("PlayAnimationButton");
            playRainbowBtn.RegisterCallback<ClickEvent>(onPlayAnimation, TrickleDown.TrickleDown);
            
            
            // Start the automatic demo actions
            StartCoroutine(timeline());
        }
        
        private void onRestartAll(ClickEvent evt)
        {
            m_WaveManipulator.Restart();
            m_SwingManipulator.Restart();
        }
        
        private void onPauseAll(ClickEvent evt)
        {
            m_WaveManipulator.Pause();
            m_SwingManipulator.Pause();
        }
        
        private void onPlayAll(ClickEvent evt)
        {
            m_WaveManipulator.Play();
            m_SwingManipulator.Play();
        }

        private void onRestartAnimation(ClickEvent evt)
        {
            var rainbowAnimation = m_WaveManipulator.GetAnimation<TextAnimationCharacter>("rainbow");
            rainbowAnimation.Restart();
        }
        
        private void onPauseAnimation(ClickEvent evt)
        {
            var rainbowAnimation = m_WaveManipulator.GetAnimation<TextAnimationCharacter>("rainbow");
            rainbowAnimation.Pause();
        }
        
        private void onPlayAnimation(ClickEvent evt)
        {
            var rainbowAnimation = m_WaveManipulator.GetAnimation<TextAnimationCharacter>("rainbow");
            rainbowAnimation.Play();
        }

        private IEnumerator timeline()
        {
            Debug.Log("-- Start of Demo --");
            
            // Let's pause the wave manipulator because by default each manipulator starts the animation immediately.
            m_WaveManipulator.Pause();
            Debug.Log("Pausing wave text.");

            yield return new WaitForSeconds(2f);
            m_WaveManipulator.Play();
            Debug.Log("Resuming wave text.");

            yield return new WaitForSeconds(0.5f);
            m_WaveManipulator.Pause();
            Debug.Log("Pausing again.");

            yield return new WaitForSeconds(2f);
            m_WaveManipulator.Play();
            Debug.Log("Resuming again.");

            yield return new WaitForSeconds(4f);
            float time = 0.5f;
            m_WaveManipulator.Restart(paused: false, time: time);
            Debug.Log("Restarting at time: " + time);

            yield return new WaitForSeconds(2);
            var rainbowAnimation = m_WaveManipulator.GetAnimation<TextAnimationCharacter>("rainbow");
            rainbowAnimation.Speed = 2f;
            Debug.Log("Speeding up the rainbow");

            yield return new WaitForSeconds(2f);
            rainbowAnimation.Pause();
            Debug.Log("Pause the rainbow");

            yield return new WaitForSeconds(2f);
            rainbowAnimation.Speed = 0.3f;
            // Notice nice we have changed the speed we use Restart() with
            // the time parameter to ensure consistency (old speed / new speed).
            rainbowAnimation.Restart(time: rainbowAnimation.Time * (2f / 0.3f));
            Debug.Log("Resume rainbow at normal speed");
            
            Debug.Log("-- End of Demo --");
        }
    }
}
