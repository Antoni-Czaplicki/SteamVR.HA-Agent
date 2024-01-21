﻿using Home_Assistant_Agent_for_SteamVR.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home_Assistant_Agent_for_SteamVR
{
    class Payload
    {
        // General
        public string imageData = "";
        public string imagePath = "";

        // Standard notification
        public string basicTitle = "Home_Assistant_Agent_for_SteamVR";
        public string basicMessage = "";

        // Custom notification
        public CustomProperties customProperties = new CustomProperties();

        public class CustomProperties {
            public bool enabled = false;
            public string nonce = "";
            public int anchorType = 1; // 0: World, 1: Head, 2: Left Hand, 3: Right Hand
            public bool attachToAnchor = false; // Fixes the overlay to the anchor
            public bool ignoreAnchorYaw = false;
            public bool ignoreAnchorPitch = false;
            public bool ignoreAnchorRoll = false;

            public int overlayChannel = 0;
            public int animationHz = -1;
            public int durationMs = 5000;
            public float opacityPer = 1;

            public float widthM = 1;
            public float zDistanceM = 1;
            public float yDistanceM = 0;
            public float xDistanceM = 0;

            public float yawDeg = 0;
            public float pitchDeg = 0;
            public float rollDeg = 0;

            public Follow follow = new Follow();
            public Animation[] animations = new Animation[0];
            public Transition[] transitions = new Transition[0];
            public TextArea[] textAreas = new TextArea[0];
        }

        public class Follow
        {
            public bool enabled = false;
            public float triggerAngle = 65; // Triggering cone angle
            public float durationMs = 250; // Transition duration
            public int tweenType = 5; // Tween type
        }

        public class Animation
        {
            public int property = 0; // 0: None (disabled), 1: Yaw, 2: Pitch, 3: Roll, 4: Z, 5: Y, 6: X, 7: Scale, 8: Opacity
            public float amplitude = 1;
            public float frequency = 1;
            public int phase = 0; // 0: Sine, 1: Cosine, 2: Negative Sine, 3: Negative Cosine
            public int waveform = 0; // 0: PhaseBased
            public bool flipWaveform = false;
        }

        public class Transition {
            public float scalePer = 1;
            public float opacityPer = 0;
            public float zDistanceM = 0; // Translational offset
            public float yDistanceM = 0; // Translational offset
            public float xDistanceM = 0; // Translational offset
            public float yawDeg = 0;
            public float pitchDeg = 0;
            public float rollDeg = 0;
            public int durationMs = 250;
            public int tweenType = 5; // Tween type
        }

        public class TextArea {
            public string text = "";
            public int xPositionPx = 0;
            public int yPositionPx = 0;
            public int widthPx = 100;
            public int heightPx = 100;
            public int fontSizePt = 10;
            public string fontFamily = "";
            public string fontColor = "";
            public int horizontalAlignment = 0; // 0: Left, 1: Center, 2: Right
            public int verticalAlignment = 0; // 0: Left, 1: Center, 2: Right
        }
    }
}