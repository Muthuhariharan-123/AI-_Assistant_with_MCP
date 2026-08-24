import 'dart:convert';
import 'package:http/http.dart' as http;

/// HTTP client for communicating with the ASP.NET Core backend.
class ApiClient {
  final String baseUrl;
  final http.Client _client;

  ApiClient({
    required this.baseUrl,
    http.Client? client,
  }) : _client = client ?? http.Client();

  /// Sends a chat message to the backend and returns the response.
  ///
  /// Returns a map with 'reply' (String) and optionally 'toolUsed' (String?).
  /// Throws [ApiException] on failure.
  Future<Map<String, dynamic>> sendMessage(String message) async {
    final uri = Uri.parse('$baseUrl/api/chat');

    try {
      final response = await _client.post(
        uri,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'message': message}),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else if (response.statusCode == 400) {
        final body = jsonDecode(response.body) as Map<String, dynamic>;
        throw ApiException(body['error'] as String? ?? 'Invalid request');
      } else if (response.statusCode == 429) {
        throw ApiException('Too many requests. Please wait a moment and try again.');
      } else {
        throw ApiException('Server error (${response.statusCode}). Please try again.');
      }
    } on http.ClientException {
      throw ApiException('Could not connect to the server. Is it running?');
    } on FormatException {
      throw ApiException('Received an invalid response from the server.');
    }
  }

  void dispose() {
    _client.close();
  }
}

/// Exception thrown when an API call fails.
class ApiException implements Exception {
  final String message;
  ApiException(this.message);

  @override
  String toString() => message;
}
