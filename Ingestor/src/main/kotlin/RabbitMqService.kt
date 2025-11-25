package org.rasmus

import com.rabbitmq.client.AMQP
import com.rabbitmq.client.Connection
import com.rabbitmq.client.ConnectionFactory
import serilogj.Log
import java.util.Date
import java.util.UUID

object RabbitMqService {
    private const val QUEUE_NAME = "ingestor.queue"

    private val factory = ConnectionFactory().apply {
        host = System.getenv("RABBITMQ_HOST") ?: "localhost"
        println(host)
        port = (System.getenv("RABBITMQ_PORT") ?: "5674").toInt()
        println(port)
        username = System.getenv("RABBITMQ_USER") ?: "guest"
        println(username)
        password = System.getenv("RABBITMQ_PASS") ?: "guest"
        println(password)
    }

    fun connectWithRetry(factory: ConnectionFactory, retries: Int = 10, delayMs: Long = 2000): Connection {
        repeat(retries) { i ->
            try {
                println("Trying to connect to RabbitMQ (attempt ${i + 1})...")
                return factory.newConnection()
            } catch (e: Exception) {
                println("Connection failed: ${e.message}, retrying in $delayMs ms")
                Thread.sleep(delayMs)
            }
        }
        throw RuntimeException("Could not connect to RabbitMQ after $retries retries")
    }

    val connection = connectWithRetry(factory)
    private val channel by lazy { connection.createChannel() }

    fun init() {
        channel.queueDeclare(QUEUE_NAME, true, false, false, null)
        println("RabbitMQ queue declared: $QUEUE_NAME")
    }

    fun publish(message: String, traceId: String) {
        val headers = HashMap<String, Any>()
        headers["TraceId"] = traceId

        val props = AMQP.BasicProperties.Builder()
            .timestamp(Date())
            .headers(headers)
            .build()

        channel.basicPublish("", QUEUE_NAME, props, message.toByteArray())
        Log.information("Message is sent to queue")
    }
}