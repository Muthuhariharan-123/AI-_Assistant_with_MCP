/// Represents a single chat message (user or AI).
class ChatMessage {
  final String text;
  final bool isUser;
  final String? toolUsed;
  final DateTime timestamp;

  ChatMessage({
    required this.text,
    required this.isUser,
    this.toolUsed,
    DateTime? timestamp,
  }) : timestamp = timestamp ?? DateTime.now();

  /// Creates a user message.
  factory ChatMessage.user(String text) {
    return ChatMessage(text: text, isUser: true);
  }

  /// Creates an AI assistant message.
  factory ChatMessage.assistant(String text, {String? toolUsed}) {
    return ChatMessage(text: text, isUser: false, toolUsed: toolUsed);
  }
}
