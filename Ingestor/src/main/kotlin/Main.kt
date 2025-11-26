package org.rasmus

import com.squareup.moshi.JsonAdapter
import com.squareup.moshi.Moshi
import com.squareup.moshi.adapter
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import serilogj.Log
import serilogj.LoggerConfiguration
import serilogj.core.enrichers.FixedPropertyEnricher
import serilogj.events.LogEventProperty
import serilogj.events.ScalarValue
import java.util.concurrent.TimeUnit

import serilogj.sinks.seq.SeqSinkConfigurator.*
import java.util.UUID
import kotlin.math.log

val WeakAppBaseUrl = System.getenv("WEAKAPP_BASE_URL") ?: "http://localhost:8080"
val SeqUrl = System.getenv("SEQ_URL") ?: "http://localhost:5341"
val Headers = mapOf("X-Api-Key" to "supersecret")

const val sleepDuration: Long = 10_000

fun getLogger(): LoggerConfiguration {
    return LoggerConfiguration()
        .writeTo(seq(SeqUrl))
        .with(
            FixedPropertyEnricher(LogEventProperty(
                "ApplicationType",
                ScalarValue("Ingestor"))))
}

@OptIn(ExperimentalStdlibApi::class)
fun main() {
    Thread.setDefaultUncaughtExceptionHandler { _, e ->
        Log.fatal(e, "Uncaught exception")
        Log.closeAndFlush()
        kotlin.system.exitProcess(1)
    }

    Log.setLogger(getLogger().createLogger())

    Log.information("Hello from {lang}!", "Java")

    RabbitMqService.init()

    val moshi: Moshi = Moshi.Builder().build()
    val jsonAdapter: JsonAdapter<List<Any>> = moshi.adapter<List<Any>>()

    val client = OkHttpClient().newBuilder().readTimeout(4, TimeUnit.SECONDS).build()

    var requestBuilder = Request.Builder()
        .url("$WeakAppBaseUrl/meters")

    for (header in Headers) {
        requestBuilder = requestBuilder.header(header.key, header.value)
    }

    val request = requestBuilder.build()

    while (true) {
        try {
            client.newCall(request).execute().use { response ->
                handleWeakAppResponse(response, jsonAdapter)
            }
        } catch (e: Exception) {
            println("Failed to send WeakApp request: ${e.message}")
            Thread.sleep(sleepDuration)
            continue;
        }


        Thread.sleep(sleepDuration)
    }
}

fun handleWeakAppResponse(response: Response, jsonAdapter: JsonAdapter<List<Any>>) {
    if (response.code == 429) {
        val timeToSleep = response.header("Retry-After")?.toLong()
        if (timeToSleep != null) {
            println("WeakApp returned 429 status code. Sleeping more.${timeToSleep}")
            Thread.sleep(sleepDuration * 1000)
            return
        }
    }

    if (!response.isSuccessful) {
        println("WeakApp returned unsuccessful status code. Retrying in 3 seconds")
        Thread.sleep(3_000)
        return
    }

    try {
        val body = response.body.string()

        if (body == "") {
            println("WeakApp returned nothing. Skipping.")
            Thread.sleep(sleepDuration)
            return
        }

        val traceId = UUID.randomUUID().toString()

        val loggerTraceConfig = getLogger().with(
            FixedPropertyEnricher(LogEventProperty(
                "TraceId",
                ScalarValue(traceId))))

        Log.setLogger(loggerTraceConfig.createLogger())

        val items = jsonAdapter.fromJson(body)

        if (items == null) {
            Log.warning("Failed to parse JSON array. Skipping.")
            Thread.sleep(sleepDuration)
            return
        } else {
            Log.information("Received array with ${items.size} elements")
            RabbitMqService.publish(body, traceId)
        }
    } catch (e: Exception) {
        Log.warning("Failed to deserialize JSON array: ${e.message}")
        Thread.sleep(sleepDuration)
        return
    }
}