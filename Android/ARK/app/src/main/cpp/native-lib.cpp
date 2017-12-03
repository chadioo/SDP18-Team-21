#include <jni.h>
#include <string>

extern "C"
jstring
Java_com_team21_sdp18_umass_ece_ark_MainMenu_stringFromJNI(
        JNIEnv* env,
        jobject /* this */) {
    std::string hello = "Hello from C++";
    return env->NewStringUTF(hello.c_str());
}
