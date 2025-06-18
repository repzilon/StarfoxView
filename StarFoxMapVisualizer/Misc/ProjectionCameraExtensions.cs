using System.Windows.Input;
using System.Windows.Media.Media3D;

using static System.Windows.Input.Key;
using static System.Windows.Input.ModifierKeys;

namespace StarFoxMapVisualizer.Misc
{
    public static class ProjectionCameraExtensions
    {
        public static TCamera Move<TCamera>(this TCamera camera, Vector3D axis, double step)
            where TCamera : ProjectionCamera
        {
            camera.Position += axis * step;
            return camera;
        }

        public static TCamera Rotate<TCamera>(this TCamera camera, Vector3D axis, double angle)
            where TCamera : ProjectionCamera
        {
            var matrix3D = new Matrix3D();
            matrix3D.RotateAt(new Quaternion(axis, angle), camera.Position);
            camera.LookDirection *= matrix3D;
            return camera;
        }

        public static Vector3D GetYawAxis(this ProjectionCamera camera) => camera.UpDirection;
        public static Vector3D GetRollAxis(this ProjectionCamera camera) => camera.LookDirection;
        public static Vector3D GetPitchAxis(this ProjectionCamera camera) => Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection);

        public static PerspectiveCamera MoveBy(this PerspectiveCamera camera, Key key) => camera.MoveBy(key, camera.FieldOfView / 180d);
        public static PerspectiveCamera RotateBy(this PerspectiveCamera camera, Key key) => camera.RotateBy(key, camera.FieldOfView / 45d);

        public static TCamera MoveBy<TCamera>(this TCamera camera, Key key, double step) where TCamera : ProjectionCamera
        {
            if (key == W) {
                return camera.Move(Keyboard.Modifiers.HasFlag(Shift) ? camera.GetYawAxis() : camera.GetRollAxis(), +step);
            } else if (key == S) {
                return camera.Move(Keyboard.Modifiers.HasFlag(Shift) ? camera.GetYawAxis() : camera.GetRollAxis(), -step);
            } else if (key == A) {
                return camera.Move(camera.GetPitchAxis(), +step);
            } else if (key == D) {
                return camera.Move(camera.GetPitchAxis(), -step);
            }
            return camera;
        }

        public static TCamera RotateBy<TCamera>(this TCamera camera, Key key, double angle) where TCamera : ProjectionCamera
        {
            if (key == Left) {
                return camera.Rotate(camera.GetYawAxis(), +angle);
            } else if (key == Right) {
                return camera.Rotate(camera.GetYawAxis(), -angle);
            } else if (key == Down) {
                return camera.Rotate(camera.GetPitchAxis(), +angle);
            } else if (key == Up) {
                return camera.Rotate(camera.GetPitchAxis(), -angle);
            }
            return camera;
        }
    }
}
