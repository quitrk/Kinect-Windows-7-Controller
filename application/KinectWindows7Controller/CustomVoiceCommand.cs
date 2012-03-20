//-----------------------------------------------------------------------
// <copyright file="CustomVoiceCommand.cs" company="WPA">
//     Copyright (c) WPA. All rights reserved.
// </copyright>
// <author>Tudor</author>
//-----------------------------------------------------------------------

namespace KinectWindows7Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class CustomVoiceCommand
    {
        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomVoiceCommand"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="sound">The sound.</param>
        public CustomVoiceCommand(string path, string sound)
        {
            this.CommandPath = path;
            this.CommandSound = sound;
        }
        #endregion

        #region PROPERTIES
        /// <summary>
        /// Gets or sets the name of the command.
        /// </summary>
        /// <value>
        /// The name of the command.
        /// </value>
        public string CommandPath { get; set; }

        /// <summary>
        /// Gets or sets the command sound.
        /// </summary>
        /// <value>
        /// The command sound.
        /// </value>
        public string CommandSound { get; set; }
        #endregion
    }
}
