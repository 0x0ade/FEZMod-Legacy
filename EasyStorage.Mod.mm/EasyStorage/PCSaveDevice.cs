#if FNA
using SDL2;
#endif

namespace EasyStorage {
    public class PCSaveDevice {
        
        #if FNA
        private static string orig_GetLocalSaveFolder() {
            return null;
        }
        
        private static string GetLocalSaveFolder() {
            if (SDL.SDL_GetPlatform() == "Android") {
                return "./FEZSAVES";
            }
            return orig_GetLocalSaveFolder();
        }
        #endif

    }
}

