#if FNA
using SDL2;
#endif

namespace Common {
    public class Util {
        
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
        
        private static string orig_GetLocalConfigFolder() {
            return null;
        }
        
        private static string GetLocalConfigFolder() {
            if (SDL.SDL_GetPlatform() == "Android") {
                return "./FEZCONFIG";
            }
            return orig_GetLocalConfigFolder();
        }
        #endif

    }
}

