﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StarFoxMapVisualizer.Controls2
{
	/// <summary>
	/// Interaction logic for CommunicationMessageControl.xaml
	/// </summary>
	public partial class CommunicationMessageControl : ContentControl, INotifyPropertyChanged
	{
		public Rect ImageRect { get; set; } = new Rect(0, 0, 31, 39);
		public Visibility MugshotVisibility { get; set; } = Visibility.Collapsed;
		public FontFamily GameFont { get; set; }

		public enum Characters
		{
			FOX,
			FALCON,
			RABBIT,
			FROG,
			PEPPER,
			ANDROSS,
			BETA_SLIPPY
		}

		public static Characters MapSpeakerToCharacter(string Speaker)
		{
			if (Speaker == "fox" || Speaker == "fox3") {
				return Characters.FOX;
			} else if (Speaker == "falcon" || Speaker == "falcon3") {
				return Characters.FALCON;
			} else if (Speaker == "rabbit" || Speaker == "rabbit3") {
				return Characters.RABBIT;
			} else if (Speaker == "frog" || Speaker == "frog3") {
				return Characters.FROG;
			} else if (Speaker == "andross" || Speaker == "andross3") {
				return Characters.ANDROSS;
			} else if (Speaker == "pepper" || Speaker == "pepper3") {
				return Characters.PEPPER;
			} else {
				return Characters.BETA_SLIPPY;
			}
		}

		public CommunicationMessageControl()
		{
			InitializeComponent();
			SetCompatibleFonts();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void DrawMugshot(Characters Character, int Frame = 0)
		{
			int baseX = 0;
			int baseY = 0;
			const int charWidth = 31;
			const int charHeight = 39;// FOX FRAME 1
			switch (Character) {
				case Characters.FOX: break;
				case Characters.FALCON: baseX = (charWidth + 1) * 2; break;
				case Characters.RABBIT: baseY = charHeight + 1; break;
				case Characters.FROG: baseX = (charWidth + 1) * 2; baseY = charHeight + 1; break;
				case Characters.PEPPER: baseY = (charHeight + 1) * 2; break;
				case Characters.ANDROSS: baseX = (charWidth + 1) * 2; baseY = (charHeight + 1) * 2; break;
				default:
					ImageMissing();
					return;
			}

			MugshotVisibility = Visibility.Visible;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MugshotVisibility)));

			ImageRect = new Rect(baseX + Frame * (charWidth + 1), baseY, charWidth, charHeight);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageRect)));
		}

		public void SetCompatibleFonts(bool Compatible = true)
		{
			GameFont = Compatible ? new FontFamily("Meiryo") : FindResource("SFFont") as FontFamily;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameFont)));
		}

		private void ImageMissing()
		{
			MugshotVisibility = Visibility.Hidden;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MugshotVisibility)));
		}
	}
}
