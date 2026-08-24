import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:ai_assistant/core/api_client.dart';
import 'package:ai_assistant/features/chat/chat_message.dart';

/// Provides the API client instance.
/// In Docker, the backend runs at the same host on port 5000.
/// For local dev, adjust the URL as needed.
final apiClientProvider = Provider<ApiClient>((ref) {
  // When running as a Docker web build served by nginx,
  // the browser's origin is localhost:8080. The backend is at localhost:5000.
  // When running locally for dev, adjust this.
  const baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5000',
  );
  return ApiClient(baseUrl: baseUrl);
});

/// State for the chat feature.
class ChatState {
  final List<ChatMessage> messages;
  final bool isLoading;
  final String? error;

  const ChatState({
    this.messages = const [],
    this.isLoading = false,
    this.error,
  });

  ChatState copyWith({
    List<ChatMessage>? messages,
    bool? isLoading,
    String? error,
  }) {
    return ChatState(
      messages: messages ?? this.messages,
      isLoading: isLoading ?? this.isLoading,
      error: error,
    );
  }
}

/// Manages the chat state — message list, loading indicator, errors.
class ChatNotifier extends StateNotifier<ChatState> {
  final ApiClient _apiClient;

  ChatNotifier(this._apiClient) : super(const ChatState());

  /// Sends a user message and waits for the AI response.
  Future<void> sendMessage(String text) async {
    if (text.trim().isEmpty) return;

    final userMessage = ChatMessage.user(text.trim());

    // Add user message and set loading
    state = state.copyWith(
      messages: [...state.messages, userMessage],
      isLoading: true,
      error: null,
    );

    try {
      final response = await _apiClient.sendMessage(text.trim());

      final aiMessage = ChatMessage.assistant(
        response['reply'] as String? ?? 'No response received.',
        toolUsed: response['toolUsed'] as String?,
      );

      state = state.copyWith(
        messages: [...state.messages, aiMessage],
        isLoading: false,
      );
    } on ApiException catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: e.message,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'An unexpected error occurred. Please try again.',
      );
    }
  }

  /// Clears the error message.
  void clearError() {
    state = state.copyWith(error: null);
  }
}

/// Provider for the chat notifier.
final chatProvider = StateNotifierProvider<ChatNotifier, ChatState>((ref) {
  final apiClient = ref.watch(apiClientProvider);
  return ChatNotifier(apiClient);
});
