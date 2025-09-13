using System;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using StarFox.Interop.MAP.EVT;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer.Controls
{
	public partial class MapEventNodeControl : HeaderedContentControl
	{
		public MAPEvent MapEvent { get; private set; }

		public MapEventNodeControl()
		{
			InitializeComponent();
		}

		public MapEventNodeControl(MAPEvent mapEvent) : this()
		{
			Attach(mapEvent);
		}

		private HeaderedContentControl CreateComponentSelection<T>(MAPEvent mapEvent, string header, string content,
		IImmutableSolidColorBrush background = null)
		{
			var groupBox = new HeaderedContentControl() {
				Header  = header,
				Content = content,
				// TODO : Cursor.Hand
				//Cursor  = Cursors.Hand
			};
			groupBox.PointerReleased += async delegate {
				await EDITORStandard.MapEditor_MapNodeSelected(mapEvent, typeof(T));
			};
			if (background != null) {
				groupBox.Background = background;
			}

			ComponentsStack.Children.Add(groupBox);
			return groupBox;
		}

		public void Attach(MAPEvent mapEvent)
		{
			this.MapEvent = mapEvent;
			ComponentsStack.Children.Clear();
			Header = mapEvent.EventName ?? "#REF!";

			if (mapEvent is MAPUnknownEvent unknown) {
				foreach (var param in unknown.MacroParameters)
					ComponentsStack.Children.Add(new HeaderedContentControl() {
						Header  = param.ParameterName?.ToUpper() ?? "PARAM",
						Content = param.ParameterContent,
					});
				BorderBrush = Brushes.Red;
				return;
			}

			if (mapEvent is IMAPNamedEvent name) {
				CreateComponentSelection<IMAPNamedEvent>(mapEvent, "NAME", name.Name);
			} else if (mapEvent is IMAPValueEvent value) {
				CreateComponentSelection<IMAPValueEvent>(mapEvent, "VALUE", value.Value);
			} else if (mapEvent is IMAPDelayEvent delay) {
				CreateComponentSelection<IMAPDelayEvent>(mapEvent, "DELAY", delay.Delay.ToString());
			} else if (mapEvent is IMAPLocationEvent loc) {
				CreateComponentSelection<IMAPLocationEvent>(mapEvent, "X", loc.X.ToString());
				CreateComponentSelection<IMAPLocationEvent>(mapEvent, "Y", loc.Y.ToString());
				CreateComponentSelection<IMAPLocationEvent>(mapEvent, "Z", loc.Z.ToString());
			} else if (mapEvent is IMAPShapeEvent shape) {
				CreateComponentSelection<IMAPShapeEvent>(mapEvent, "SHAPE", shape.ShapeName);
			} else if (mapEvent is IMAPStrategyEvent strat) {
				CreateComponentSelection<IMAPStrategyEvent>(mapEvent, "STRATEGY", strat.StrategyName);
			} else if (mapEvent is IMAPPathEvent path) {
				CreateComponentSelection<IMAPPathEvent>(mapEvent, "PATH", path.PathName);
			} else if (mapEvent is IMAPHealthAttackEvent hp) {
				CreateComponentSelection<IMAPHealthAttackEvent>(mapEvent, "Health Points", hp.HP.ToString());
				CreateComponentSelection<IMAPHealthAttackEvent>(mapEvent, "Attack Power", hp.AP.ToString());
			}

			ComponentsStack.Children.Add(new HeaderedContentControl() {
				Header  = "LINE",
				Content = mapEvent.Callsite.Line
			});
			ComponentsStack.Children.Add(new HeaderedContentControl() {
				Header  = "LEVEL TIME",
				Content = mapEvent.LevelDelay.ToString()
			});
			if (mapEvent is IMAPBGEvent) {
				CreateComponentSelection<IMAPBGEvent>(mapEvent, "THEME", "preview theme", Brushes.SlateBlue);
			}
		}
	}
}
