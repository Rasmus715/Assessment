package org.rasmus

import okhttp3.OkHttpClient
import okhttp3.Request

//TIP To <b>Run</b> code, press <shortcut actionId="Run"/> or
// click the <icon src="AllIcons.Actions.Execute"/> icon in the gutter.
fun main() {
    val client = OkHttpClient()

    val request = Request.Builder()
        .url("https://api.example.com/data")
        .build()

    client.newCall(request).execute().use { response ->
        if (!response.isSuccessful) {
            throw Exception("Unexpected code $response")
        }

        println(response.body.string())
    }
}