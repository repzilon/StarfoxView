﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using StarFox.Interop;
using StarFox.Interop.EFFECTS;
using StarFox.Interop.MAP.CONTEXT;

namespace StarFoxMapVisualizer.Renderers
{
    /// <summary>
    /// Interaction logic for BackgroundSkyRenderer.xaml
    /// </summary>
    public partial class BackgroundSkyRenderer : SCRRendererControlBase
    {
        const int RENDER_W = StarfoxEqu.RENDER_W, RENDER_H = StarfoxEqu.RENDER_H, SCR_W = StarfoxEqu.SCR_W, SCR_H = StarfoxEqu.SCR_H;
        double BackgroundX, BackgroundY, ViewportWidth, ViewportHeight;

        public BackgroundSkyRenderer()
        {
            InitializeComponent();

            BackgroundX = (SCR_W / 2) - (RENDER_W / 2);
            BackgroundY = (SCR_H / 2) - (RENDER_H / 2);
            ViewportWidth = SCR_W;
            ViewportHeight = SCR_H;

            UpdateViewport();
        }

        public BackgroundSkyRenderer(MAPContextDefinition levelContext) : this()
        {
            LevelContext = levelContext;

            _ = SetContext(levelContext);
        }

        void UpdateViewport()
        {
            BackgroundBrush.Viewbox = new Rect(
                0, 0, ViewportWidth, ViewportHeight);
            BackgroundBrush.Viewport = new Rect(
                BackgroundX, -BackgroundY, SCR_W, SCR_H);
        }

        public void ScrollToCamera(PerspectiveCamera Camera)
        {
            //BackgroundX = (SCR_W / 2) - (RENDER_W / 2) + (-LookAt.X * SCR_W);

            var LookAt = Camera.LookDirection;
            double FOV = Camera.FieldOfView;

            ViewportWidth = SCR_W;
            ViewportHeight = SCR_H;

            double halfSCRW = SCR_W / 2, screenYBound = SCR_H - RENDER_H;

            Point pixelpos = new Point(LookAt.Z, LookAt.X);
            double xRotation = Math.Atan2(pixelpos.Y, pixelpos.X) * (FOV / 100);
            double yRotation = -LookAt.Y * .5;

            double YOffset = LevelContext?.BG2.VerticalOffset ?? (.235 * SCR_H);

            double desiredY = ((SCR_H + YOffset) / 2) - (RENDER_H / 2) + (yRotation * screenYBound);
            if (desiredY > screenYBound)
                ViewportHeight -= desiredY - screenYBound;

            BackgroundX = SCR_W + (xRotation * halfSCRW);
            BackgroundY = desiredY;
            UpdateViewport();
        }

        public override async Task SetContext(MAPContextDefinition SelectedContext,
            WavyBackgroundRenderer.WavyEffectStrategies Animation = WavyBackgroundRenderer.WavyEffectStrategies.None,
            bool ExtractCCR = false, bool ExtractPCR = false)
        {
            LevelContext = SelectedContext;

            BackgroundX = (SCR_W / 2) - (RENDER_W / 2);
            BackgroundY = (SCR_H / 2) - (RENDER_H / 2);
            ViewportWidth = SCR_W;
            ViewportHeight = SCR_H;

            UpdateViewport();

            if (LevelContext != null)
                await InvalidateBGS();
        }

        public override void BG2Invalidate(ImageSource NewImage) => BackgroundBrush.ImageSource = NewImage;

        public override void BG3Invalidate(ImageSource NewImage)
        {
            ;
        }
    }
}
