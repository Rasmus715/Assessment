package org.rasmus

import com.rabbitmq.client.ConnectionFactory

object RabbitMqService {
    private const val QUEUE_NAME = "ingestor.queue"

    private val factory = ConnectionFactory().apply {
        host = System.getenv("RABBITMQ_HOST") ?: "localhost"
        port = (System.getenv("RABBITMQ_PORT") ?: "5674").toInt()
        username = System.getenv("RABBITMQ_USER") ?: "guest"
        password = System.getenv("RABBITMQ_PASS") ?: "guest"
    }

    private val connection by lazy { factory.newConnection("weakapp-connection") }
    private val channel by lazy { connection.createChannel() }

    fun init() {
        channel.queueDeclare(QUEUE_NAME, true, false, false, null)
        println("RabbitMQ queue declared: $QUEUE_NAME")
    }

    fun publish(message: String) {
        channel.basicPublish("", QUEUE_NAME, null, message.toByteArray())
        println("Message is sent to queue")
    }
}