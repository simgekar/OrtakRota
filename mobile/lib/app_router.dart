import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import 'screens/join_trip_screen.dart';
import 'screens/login_screen.dart';
import 'screens/register_screen.dart';
import 'screens/trip_detail_screen.dart';
import 'screens/trip_form_screen.dart';
import 'screens/trips_screen.dart';
import 'state/auth_provider.dart';

GoRouter createRouter(AuthProvider auth) {
  return GoRouter(
    initialLocation: '/trips',
    // Oturum durumu değişince yönlendirme kuralları yeniden çalışır
    refreshListenable: auth,
    redirect: (context, state) {
      final location = state.matchedLocation;
      final onAuthPage = location == '/login' || location == '/register';

      switch (auth.status) {
        case AuthStatus.unknown:
          return location == '/splash' ? null : '/splash';
        case AuthStatus.unauthenticated:
          return onAuthPage ? null : '/login';
        case AuthStatus.authenticated:
          return (onAuthPage || location == '/splash') ? '/trips' : null;
      }
    },
    routes: [
      GoRoute(
        path: '/splash',
        builder: (context, state) => const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        ),
      ),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: '/trips',
        builder: (context, state) => const TripsScreen(),
        // Sabit yollar (new, join) ':id' yolundan önce tanımlanmalı
        routes: [
          GoRoute(
            path: 'new',
            builder: (context, state) => const TripFormScreen(),
          ),
          GoRoute(
            path: 'join',
            builder: (context, state) => const JoinTripScreen(),
          ),
          GoRoute(
            path: ':id',
            builder: (context, state) {
              final id = int.parse(state.pathParameters['id']!);
              return TripDetailScreen(tripId: id);
            },
            routes: [
              GoRoute(
                path: 'edit',
                builder: (context, state) {
                  final id = int.parse(state.pathParameters['id']!);
                  return TripFormScreen(tripId: id);
                },
              ),
            ],
          ),
        ],
      ),
    ],
  );
}
