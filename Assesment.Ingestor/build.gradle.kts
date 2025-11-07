plugins {
    kotlin("jvm") version "2.2.20"
}

group = "org.rasmus"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
}

dependencies {
    testImplementation(kotlin("test"))
    implementation("com.squareup.okhttp3:okhttp:5.3.0")
}

tasks.test {
    useJUnitPlatform()
}
kotlin {
    jvmToolchain(24)
}