import 'package:go_router/go_router.dart';

import 'screens/login_screen.dart';
import 'screens/trip_detail_screen.dart';
import 'screens/trips_screen.dart';

final appRouter = GoRouter(
  initialLocation: '/login',
  routes: [
    GoRoute(
      path: '/login',
      builder: (context, state) => const LoginScreen(),
    ),
    GoRoute(
      path: '/trips',
      builder: (context, state) => const TripsScreen(),
      routes: [
        GoRoute(
          path: ':id',
          builder: (context, state) {
            final id = int.parse(state.pathParameters['id']!);
            return TripDetailScreen(tripId: id);
          },
        ),
      ],
    ),
  ],
);
