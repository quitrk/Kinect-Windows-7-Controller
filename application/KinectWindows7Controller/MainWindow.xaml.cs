//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="WPA">
//     Copyright (c) WPA. All rights reserved.
// </copyright>
// <author>Tudor</author>
//-----------------------------------------------------------------------

namespace KinectWindows7Controller
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Forms;
    using Coding4Fun.Kinect.Wpf;
    using Microsoft.Research.Kinect.Audio;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Research.Kinect.Nui;
    using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
    using System.IO;
    using WindowsInput;
    using System.Collections.Generic;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region PRIVATE FIELDS
        /// <summary>
        /// The Speech Recognition Engine field
        /// </summary>
        private SpeechRecognitionEngine sre;

        /// <summary>
        /// The kinect audio source
        /// </summary>
        private KinectAudioSource kinectSource;

        /// <summary>
        /// The kinect runtime.
        /// </summary>
        private Runtime runtime;

        /// <summary>
        /// The notify icon.
        /// </summary>
        private NotifyIcon notifyIcon;
        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += (o, e) => this.DragMove();

            this.CustomVoiceCommands = new List<CustomVoiceCommand>();

            RunInSystemTray();
        }
        #endregion

        #region PROPERTIES
        /// <summary>
        /// Gets or sets a value indicating whether this instance is mouse tracked.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is mouse tracked; otherwise, <c>false</c>.
        /// </value>
        public bool IsMouseTracked { get; set; }

        /// <summary>
        /// Gets or sets the voice commands.
        /// </summary>
        /// <value>The voice commands.</value>
        public List<CustomVoiceCommand> CustomVoiceCommands { get; set; }
        #endregion

        #region EVENT HANDLERS
        /// <summary>
        /// Handles the Click event of the BrowseNewCustomCommandPath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void BrowseNewCustomCommandPath_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { DefaultExt = ".exe", Filter = "Exe files (.exe)|*.exe" };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.newCustomCommandPath.Text = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// Handles the Click event of the AddNewCustomCommand control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddNewCustomCommand_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.newCustomCommandPath.Text) || string.IsNullOrEmpty(this.newCustomCommandSound.Text))
            {
                return;
            }

            this.CustomVoiceCommands.Add(new CustomVoiceCommand(this.newCustomCommandPath.Text, this.newCustomCommandSound.Text));
            this.newCustomCommandPath.Text = string.Empty;
            this.newCustomCommandSound.Text = string.Empty;

            this.customCommands.ItemsSource = this.CustomVoiceCommands;

            this.InitializeSpeechRecognition();
        }

        /// <summary>
        /// Called when [window loaded].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (Runtime.Kinects.Count > 0)
            {
                this.LoadPreferences();

                this.runtime = Runtime.Kinects[0];
                runtime.Initialize(RuntimeOptions.UseSkeletalTracking);

                #region SKELETON ENGINE INITIALIZATION
                runtime.SkeletonEngine.TransformSmooth = true;

                var parameters = new TransformSmoothParameters
                                     {
                                         Correction = 0.3f,
                                         Prediction = 0.5f,
                                         Smoothing = 0.05f,
                                         JitterRadius = 0.05f,
                                         MaxDeviationRadius = 0.04f
                                     };

                runtime.SkeletonEngine.SmoothParameters = parameters;
                #endregion

                this.InitializeSpeechRecognition();

                this.Hide();
            }
        }

        /// <summary>
        /// Called when [skeleton frame ready].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Microsoft.Research.Kinect.Nui.SkeletonFrameReadyEventArgs"/> instance containing the event data.</param> 
        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeleton = e.SkeletonFrame.Skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);

            if (skeleton != null)
            {
                var leftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth),
                                                                         Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight),
                                                                         0.3f,
                                                                         0.2f);
                var rightHand = skeleton.Joints[JointID.HandRight].ScaleTo(Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth),
                                                                           Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight),
                                                                           0.3f,
                                                                           0.2f);

                this.GetMouseMovements(rightHand);
                this.GetMouseClicks(skeleton.Joints[JointID.HandRight], leftHand);
            }
        }

        /// <summary>
        /// Called when [notify icon mouse click].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void OnNotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            // Show the context menu
            var contextMenu = this.TryFindResource("SysTrayContextMenu") as System.Windows.Controls.ContextMenu;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// Handles the Click event of the ExitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.notifyIcon.Visible = true;
            this.notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles the Click event of the CancelBtn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// Handles the SpeechRecognized event of the sre control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Speech.Recognition.SpeechRecognizedEventArgs"/> instance containing the event data.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text.Equals(this.mouseLeftBtn.Text))
            {
                MouseSimulator.SimulateMouseLeftClick();
            }
            else if (e.Result.Text.Equals(this.mouseRightBtn.Text))
            {
                MouseSimulator.SimulateMouseRightClick();
            }
            else if (e.Result.Text.Equals(this.mouseDoubleClick.Text))
            {
                MouseSimulator.SimulateMouseLeftClick();
                MouseSimulator.SimulateMouseLeftClick();
            }
            else if (e.Result.Text.Equals(this.scrollUp.Text))
            {
                MouseSimulator.SimulateMouseWheel(-500);
            }
            else if (e.Result.Text.Equals(this.scrollDown.Text))
            {
                MouseSimulator.SimulateMouseWheel(500);
            }
            else if (e.Result.Text.Equals(this.closeWindow.Text))
            {
                InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.F4);
            }
            else if (e.Result.Text.Equals(this.back.Text))
            {
                InputSimulator.SimulateKeyPress(VirtualKeyCode.BACK);
            }
            else if (e.Result.Text.Equals(this.minimiseWindow.Text))
            {
                InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.MENU, VirtualKeyCode.SPACE }, VirtualKeyCode.VK_N);
            }
            else if (e.Result.Text.Equals(this.maximiseWindow.Text))
            {
                InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.MENU, VirtualKeyCode.SPACE }, VirtualKeyCode.VK_X);
            }
            else if (this.CustomVoiceCommands.Any(x => x.CommandSound == e.Result.Text))
            {
                Process.Start(this.CustomVoiceCommands.First(x => x.CommandSound == e.Result.Text).CommandPath);
            }
            else if (e.Result.Text.Equals("start"))
            {
                this.InitializeMouseMovement();
            }
            else if (e.Result.Text.Equals("stop"))
            {
                this.RemoveMouseMovement();
            }
        }

        /// <summary>
        /// Called when [window closed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (this.runtime != null)
            {
                this.runtime.Uninitialize();
                this.SavePreferences();
            }
        }

        /// <summary>
        /// Handles the Click event of the PreferencesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PreferencesButton_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        /// <summary>
        /// Handles the Click event of the SaveBtn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            this.InitializeSpeechRecognition();

            this.Hide();
        }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Initializes the speech recognition.
        /// </summary>
        /// <param name="choices">The choices.</param>
        private void InitializeSpeechRecognition()
        {
            if (Runtime.Kinects.Count > 0)
            {
                var ri = GetKinectRecognizer();
                sre = new SpeechRecognitionEngine(ri.Id);

                var choices = new Choices();
                choices.Add(this.mouseLeftBtn.Text);
                choices.Add(this.mouseRightBtn.Text);
                choices.Add(this.mouseDoubleClick.Text);
                choices.Add(this.minimiseWindow.Text);
                choices.Add(this.maximiseWindow.Text);
                choices.Add(this.scrollUp.Text);
                choices.Add(this.scrollDown.Text);
                choices.Add(this.closeWindow.Text);
                choices.Add(this.back.Text);
                choices.Add("start");
                choices.Add("stop");

                foreach (var customVoiceCommand in this.CustomVoiceCommands)
                {
                    choices.Add(customVoiceCommand.CommandSound);
                }

                var gb = new GrammarBuilder();
                gb.Culture = ri.Culture;
                gb.Append(choices);

                // Create the actual Grammar instance, and then load it into the speech recognizer.
                var g = new Grammar(gb);
                sre.LoadGrammar(g);
                sre.SpeechRecognized += this.SpeechRecognized;

                kinectSource = new KinectAudioSource
                                   {
                                       SystemMode = SystemMode.OptibeamArrayOnly,
                                       FeatureMode = true,
                                       AutomaticGainControl = false,
                                       MicArrayMode = MicArrayMode.MicArrayAdaptiveBeam
                                   };

                sre.SetInputToAudioStream(kinectSource.Start(),
                    new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                sre.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        /// <summary>
        /// Initializes the mouse movement.
        /// </summary>
        private void InitializeMouseMovement()
        {
            if (!this.IsMouseTracked)
            {
                runtime.SkeletonFrameReady += this.OnSkeletonFrameReady;
                this.IsMouseTracked = true;
            }
        }

        /// <summary>
        /// Removes the mouse movement.
        /// </summary>
        private void RemoveMouseMovement()
        {
            if (this.IsMouseTracked)
            {
                runtime.SkeletonFrameReady -= this.OnSkeletonFrameReady;
                this.IsMouseTracked = false;
            }
        }

        /// <summary>
        /// Gets the kinect recognizer.
        /// </summary>
        /// <returns></returns>
        private RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        /// <summary>
        /// Gets the mouse clicks.
        /// </summary>
        /// <param name="rightHand">The right hand.</param>
        /// <param name="leftHand">The left hand.</param>
        private void GetMouseClicks(Joint rightHand, Joint leftHand)
        {
        }

        /// <summary>
        /// Gets the mouse movements.
        /// </summary>
        /// <param name="rightHand">The right hand.</param>
        private void GetMouseMovements(Joint rightHand)
        {
            MouseSimulator.SimulateMouseMove(rightHand.Position.X, rightHand.Position.Y);
        }

        /// <summary>
        /// Runs the in system tray.
        /// </summary>
        private void RunInSystemTray()
        {
            this.ShowInTaskbar = false;

            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipTitle = "Kinect Windows 7 Controller";
            this.notifyIcon.BalloonTipText = "The Kinect Windows 7 Controller is still running. Click for more options.";
            this.notifyIcon.Text = "Kinect Windows 7 Controller";
            this.notifyIcon.Icon = new System.Drawing.Icon("icon.ico");
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseClick += this.OnNotifyIconMouseClick;
        }

        /// <summary>
        /// Loads the preferences.
        /// </summary>
        private void LoadPreferences()
        {
            var voiceCommands = Properties.Settings.Default.VoiceCommands;
            var customVoiceCommands = Properties.Settings.Default.CustomVoiceCommands;
            var internalSeparator = new[] { "::" };
            var externalSeparator = new[] { "++" };

            if (string.IsNullOrEmpty(voiceCommands))
            {
                voiceCommands += "mouseleftbutton::select++";
                voiceCommands += "mouserightbutton::options++";
                voiceCommands += "mousedoubleclick::open++";
                voiceCommands += "minimise::minimise++";
                voiceCommands += "maximise::maximise++";
                voiceCommands += "close::close++";
                voiceCommands += "scrollup::up++";
                voiceCommands += "scrolldown::down++";
                voiceCommands += "back::back++";
            }

            this.mouseLeftBtn.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[0].Split(internalSeparator, StringSplitOptions.None)[1];
            this.mouseRightBtn.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[1].Split(internalSeparator, StringSplitOptions.None)[1];
            this.mouseDoubleClick.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[2].Split(internalSeparator, StringSplitOptions.None)[1];
            this.minimiseWindow.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[3].Split(internalSeparator, StringSplitOptions.None)[1];
            this.maximiseWindow.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[4].Split(internalSeparator, StringSplitOptions.None)[1];
            this.closeWindow.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[5].Split(internalSeparator, StringSplitOptions.None)[1];
            this.scrollUp.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[6].Split(internalSeparator, StringSplitOptions.None)[1];
            this.scrollDown.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[7].Split(internalSeparator, StringSplitOptions.None)[1];
            this.back.Text = voiceCommands.Split(externalSeparator, StringSplitOptions.None)[8].Split(internalSeparator, StringSplitOptions.None)[1];

            if (!string.IsNullOrEmpty(customVoiceCommands))
            {
                this.CustomVoiceCommands = new List<CustomVoiceCommand>();

                foreach (var customVoiceCommand in customVoiceCommands.Split(externalSeparator, StringSplitOptions.None))
                {
                    if (!string.IsNullOrEmpty(customVoiceCommand))
                    {
                        var commandPath = customVoiceCommand.Split(internalSeparator, StringSplitOptions.None)[0];
                        var commandSound = customVoiceCommand.Split(internalSeparator, StringSplitOptions.None)[1];
                        this.CustomVoiceCommands.Add(new CustomVoiceCommand(commandPath, commandSound));
                    }
                }

                this.customCommands.ItemsSource = this.CustomVoiceCommands;
            }
        }

        /// <summary>
        /// Saves the preferences.
        /// </summary>
        private void SavePreferences()
        {
            var voiceCommands = string.Empty;
            var customVoiceCommands = string.Empty;

            voiceCommands += "mouseleftbutton::" + this.mouseLeftBtn.Text + "++";
            voiceCommands += "mouserightbutton::" + this.mouseRightBtn.Text + "++";
            voiceCommands += "mousedoubleclick::" + this.mouseDoubleClick.Text + "++";
            voiceCommands += "minimise::" + this.minimiseWindow.Text + "++";
            voiceCommands += "maximise::" + this.maximiseWindow.Text + "++";
            voiceCommands += "close::" + this.closeWindow.Text + "++";
            voiceCommands += "scrollup::" + this.scrollUp.Text + "++";
            voiceCommands += "scrolldown::" + this.scrollDown.Text + "++";
            voiceCommands += "back::" + this.back.Text + "++";

            if(this.CustomVoiceCommands.Count > 0)
            {
                foreach (var customVoiceCommand in this.CustomVoiceCommands)
                {
                    customVoiceCommands += customVoiceCommand.CommandPath + "::" + customVoiceCommand.CommandSound + "++";
                }
            }


            Properties.Settings.Default.VoiceCommands = voiceCommands;
            Properties.Settings.Default.CustomVoiceCommands = customVoiceCommands;
            Properties.Settings.Default.Save();
        }
        #endregion
    }
}
