EXTERNAL AcceptQuest()

Hola, aventurero. He oído que eres de confianza.
Hay unos limos peligrosos al este de la ciudad que están causando problemas.
¿Podrías encargarte de 5 de ellos por mí? Te recompensaré bien.

    + [Aceptar la Misión]
        ~ temp result = AcceptQuest()
        ¡Genial! Sabía que podía contar contigo. Ten mucho cuidado y vuelve a verme cuando hayas acabado.
        -> END

    + [No, gracias. Quizás más tarde.]
        Entiendo. Es una tarea peligrosa. Si cambias de opinión, aquí estaré.
        -> END