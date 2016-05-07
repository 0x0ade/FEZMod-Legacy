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
			//TODO uhh... this thing sets the view?
		}

	}
}
