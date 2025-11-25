plugins {
    kotlin("jvm") version "2.2.20"
    id("com.gradleup.shadow") version "9.2.2"
    application
}

application {
    mainClass.set("org.rasmus.MainKt")
}

group = "org.rasmus"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
    maven {
        url = uri("https://verve.jfrog.io/artifactory/verve-gradle-release/")
    }
}

dependencies {
    testImplementation(kotlin("test"))
    implementation("com.squareup.okhttp3:okhttp:5.3.0")
    implementation("com.squareup.moshi:moshi:1.15.2")
    implementation("com.rabbitmq:amqp-client:5.21.0")
    implementation("org.slf4j:slf4j-simple:2.0.17")
    implementation("org.serilogj.serilogj:serilogj:0.5.1")
}

tasks.test {
    useJUnitPlatform()
}

kotlin {
    jvmToolchain(24)
}