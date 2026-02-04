plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
}

android {
    namespace = "iss.nus.edu.sg.sharedprefs.admobile"
    compileSdk = 36

    defaultConfig {
        applicationId = "iss.nus.edu.sg.sharedprefs.admobile"
        minSdk = 24
        targetSdk = 36
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
    kotlinOptions {
        jvmTarget = "1.8"
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.activity)
    implementation(libs.androidx.constraintlayout)
    implementation("com.airbnb.android:lottie:5.2.0")
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    implementation("com.google.android.gms:play-services-maps:18.2.0")
    implementation("com.google.maps:google-maps-services:2.2.0")
    implementation("com.google.android.libraries.places:places:3.3.0")
    implementation("androidx.constraintlayout:constraintlayout:2.1.4")
    implementation("com.github.PhilJay:MPAndroidChart:v3.1.0")
    // Retrofit 核心库
    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    // 添加 Gson 转换器（用于自动解析后端返回的 JSON）
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")
    // (可选) 添加 OkHttp 日志拦截器，方便你在控制台看请求细节
    implementation("com.squareup.okhttp3:logging-interceptor:4.10.0")
    // 使用 lifecycleScope
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.6.2")
    // 在 ViewModel 里用协程
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.6.2")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")
    // 🌟 添加 Glide 依赖
    implementation("com.github.bumptech.glide:glide:4.15.1")
    // 如果你使用的是 Kotlin，建议使用 ksp 或 kapt 替代 annotationProcessor
    annotationProcessor("com.github.bumptech.glide:compiler:4.15.1")
    implementation("com.google.zxing:core:3.5.3")

}