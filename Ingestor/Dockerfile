# Stage 1: build
FROM amazoncorretto:24 AS build
WORKDIR /app

COPY gradlew .
COPY gradle gradle
COPY build.gradle.kts .
COPY settings.gradle.kts .
COPY src src

RUN sh ./gradlew shadowJar --no-daemon

FROM eclipse-temurin:25-jre-jammy AS runtime
WORKDIR /app

COPY --from=build /app/build/libs/Assessment.Ingestor-*-all.jar app.jar

CMD ["java", "-jar", "app.jar"]