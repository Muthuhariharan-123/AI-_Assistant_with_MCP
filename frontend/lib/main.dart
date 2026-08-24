import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:ai_assistant/features/chat/chat_screen.dart';

void main() {
  runApp(
    const ProviderScope(
      child: AiAssistantApp(),
    ),
  );
}

class AiAssistantApp extends StatelessWidget {
  const AiAssistantApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'My Assistant',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        brightness: Brightness.dark,
        colorScheme: ColorScheme.dark(
          primary: const Color(0xFF6366f1),
          secondary: const Color(0xFF8b5cf6),
          surface: const Color(0xFF1e1e2e),
          onSurface: Colors.white,
        ),
        textTheme: GoogleFonts.interTextTheme(
          ThemeData.dark().textTheme,
        ),
        scaffoldBackgroundColor: const Color(0xFF0f0f1a),
      ),
      home: const ChatScreen(),
    );
  }
}
