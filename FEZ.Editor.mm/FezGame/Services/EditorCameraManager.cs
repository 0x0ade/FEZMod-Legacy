using FezEngine;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using FezGame.Editor;

namespace FezGame.Services {
	public class EditorCameraManager : GameCameraManager {
		
		//hiding already defined prop due to private getter
		[ServiceDependency]
		public IGraphicsDeviceService GraphicsDeviceService {
			get;
			set;
		}
		
		public EditorCameraManager(Game game) : base(game) {
            //no-op
		}

		protected override void DollyZoom() {
			bool isOrtho = viewpoint.IsOrthographic();
			float step = (directionTransition.TotalStep != 0f) ? directionTransition.TotalStep : 0.001f;
			float fov = MathHelper.Lerp((!isOrtho) ? 0f : DefaultCameraManager.DefaultFov, (!isOrtho) ? DefaultCameraManager.DefaultFov : 0f, step);
			float prevRadius = radiusBeforeTransition;
			if (DollyZoomOut) {
				prevRadius = radiusBeforeTransition + (1f - Easing.EaseIn((double) step, EasingType.Quadratic)) * 15f;
			}
			float radius = prevRadius / AspectRatio / (2f * (float) Math.Tan((double) (fov / 2f)));
			if (directionTransition.Reached) {
				ProjectionTransition = false;
				if (!isOrtho) {
					predefinedViews[lastViewpoint].Direction = -lastViewpoint.ForwardVector();
					current.Radius = radius;
				} else {
					current.Radius = radiusBeforeTransition;
					NearPlane = 0.25f;
					FarPlane = 500f;
				}
				FogManager.Density = !FezEditor.Settings.FogEnabled ? 0f : ((LevelManager.Sky != null) ? LevelManager.Sky.FogDensity : 0f);
				DollyZoomOut = false;
				RebuildProjection();
				SnapInterpolation();
			} else {
				FogManager.Density = !FezEditor.Settings.FogEnabled ? 0f : ((LevelManager.Sky != null) ? LevelManager.Sky.FogDensity : 0f) * Easing.EaseIn((double) ((!isOrtho) ? step : (1f - step)), EasingType.Quadratic);
				NearPlane = Math.Max(0.25f, 0.25f + radius - prevRadius);
				FarPlane = Math.Max(radius + NearPlane, 499.75f);
				FieldOfView = fov;
				projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
				OnProjectionChanged();
				current.Radius = radius;
				view = Matrix.CreateLookAt(current.Radius * current.Direction + current.Center, current.Center, Vector3.UnitY);
				OnViewChanged();
			}
            
		}

	}
}
