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
                System.IO.Directory.CreateDirectory("./FEZSAVES");
                return "./FEZSAVES";
            }
            return orig_GetLocalSaveFolder();
        }
        
        private static string orig_GetLocalConfigFolder() {
            return null;
        }
        
        private static string GetLocalConfigFolder() {
            if (SDL.SDL_GetPlatform() == "Android") {
                System.IO.Directory.CreateDirectory("./FEZCONFIG");
                return "./FEZCONFIG";
            }
            return orig_GetLocalConfigFolder();
        }
        #endif
        
    }
}

