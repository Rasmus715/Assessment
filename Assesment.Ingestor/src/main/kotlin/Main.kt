package org.rasmus

import com.squareup.moshi.JsonAdapter
import okhttp3.OkHttpClient
import okhttp3.Request

import com.squareup.moshi.Moshi
import com.squareup.moshi.adapter
import okhttp3.Response
import java.util.concurrent.TimeUnit

val WeakAppBaseUrl = System.getenv("WeakApp:BaseUrl") ?: "http://localhost:8080"
val Headers = mapOf("X-Api-Key" to "supersecret")

const val sleepDuration: Long = 10_000

@OptIn(ExperimentalStdlibApi::class)
fun main() {
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
        client.newCall(request).execute().use { response ->
            handleWeakAppResponse(response, jsonAdapter)
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
        println("WeakApp returned unsuccessful status code. Skipping.")
        Thread.sleep(sleepDuration)
        return
    }

    try {
        val body = response.body.string()

        if (body == "") {
            println("WeakApp returned nothing. Skipping.")
            Thread.sleep(sleepDuration)
            return
        }

        val items = jsonAdapter.fromJson(body)

        if (items == null) {
            println("Failed to parse JSON array. Skipping.")
            Thread.sleep(sleepDuration)
            return
        } else {
            println("Received array with ${items.size} elements")
            RabbitMqService.publish(body)
        }
    } catch (e: Exception) {
        println("Failed to deserialize JSON array: ${e.message}")
        Thread.sleep(sleepDuration)
        return
    }
}