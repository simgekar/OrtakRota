import 'package:flutter/material.dart';

import 'app_router.dart';

void main() {
  runApp(const OrtakRotaApp());
}

class OrtakRotaApp extends StatelessWidget {
  const OrtakRotaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'OrtakRota',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorSchemeSeed: const Color(0xFF0F766E),
        useMaterial3: true,
        inputDecorationTheme: const InputDecorationTheme(
          border: OutlineInputBorder(),
        ),
      ),
      routerConfig: appRouter,
    );
  }
}
