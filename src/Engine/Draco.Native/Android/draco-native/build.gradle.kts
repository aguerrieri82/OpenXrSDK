plugins {
    id("com.android.library")
}

android {
    namespace = "net.eusoft.draconative"
    compileSdk = 34

    defaultConfig {
        minSdk = 24
        ndk{
            abiFilters.add("arm64-v8a")
        }
    }

    externalNativeBuild {
        cmake {
            path("src/CMakeLists.txt")
            version = "3.22.1"
        }
    }
}

dependencies {


}