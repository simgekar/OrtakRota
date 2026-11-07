import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../models/trip.dart';
import '../services/trip_service.dart';
import '../state/auth_provider.dart';

class TripsScreen extends StatefulWidget {
  const TripsScreen({super.key});

  @override
  State<TripsScreen> createState() => _TripsScreenState();
}

class _TripsScreenState extends State<TripsScreen> {
  late Future<List<Trip>> _tripsFuture;

  @override
  void initState() {
    super.initState();
    _tripsFuture = context.read<TripService>().getTrips();
  }

  Future<void> _refresh() async {
    if (!mounted) return;
    setState(() {
      _tripsFuture = context.read<TripService>().getTrips();
    });
    await _tripsFuture.catchError((_) => <Trip>[]);
  }

  Future<void> _createTrip() async {
    final created = await context.push<bool>('/trips/new');
    if (created == true && mounted) _refresh();
  }

  // Davet koduyla katılınca liste yenilenir ve katılınan gezinin detayı açılır
  Future<void> _joinTrip() async {
    final tripId = await context.push<int>('/trips/join');
    if (tripId == null || !mounted) return;
    _refresh();
    await context.push('/trips/$tripId');
    if (mounted) _refresh();
  }

  Future<void> _openTrip(int id) async {
    await context.push('/trips/$id');
    // Detayda düzenleme, silme veya ayrılma olmuş olabilir
    if (mounted) _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Gezilerim'),
            if (user != null)
              Text(
                user.isAdmin ? '${user.fullName} · Yönetici' : user.fullName,
                style: Theme.of(context).textTheme.bodySmall,
              ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Koda katıl',
            icon: const Icon(Icons.group_add_outlined),
            onPressed: _joinTrip,
          ),
          IconButton(
            tooltip: 'Yenile',
            icon: const Icon(Icons.refresh),
            onPressed: _refresh,
          ),
          IconButton(
            tooltip: 'Çıkış',
            icon: const Icon(Icons.logout),
            // Çıkış yapılınca router kullanıcıyı giriş ekranına yönlendirir
            onPressed: () => context.read<AuthProvider>().logout(),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _createTrip,
        icon: const Icon(Icons.add),
        label: const Text('Yeni Gezi'),
      ),
      body: FutureBuilder<List<Trip>>(
        future: _tripsFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return _ErrorView(
              message: TripService.errorMessage(snapshot.error!),
              onRetry: _refresh,
            );
          }

          final trips = snapshot.data!;
          if (trips.isEmpty) {
            return RefreshIndicator(
              onRefresh: _refresh,
              child: ListView(
                children: const [
                  SizedBox(height: 120),
                  Icon(Icons.luggage_outlined, size: 64),
                  SizedBox(height: 12),
                  Text('Henüz bir gezin yok.', textAlign: TextAlign.center),
                  SizedBox(height: 4),
                  Text('"Yeni Gezi" ile oluştur ya da davet koduyla katıl.',
                      textAlign: TextAlign.center),
                ],
              ),
            );
          }

          return RefreshIndicator(
            onRefresh: _refresh,
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 700),
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(12, 12, 12, 88),
                  itemCount: trips.length,
                  itemBuilder: (context, index) => _TripCard(
                    trip: trips[index],
                    onTap: () => _openTrip(trips[index].id),
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

class _TripCard extends StatelessWidget {
  const _TripCard({required this.trip, required this.onTap});

  final Trip trip;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dateFormat = DateFormat('dd.MM.yyyy');

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(trip.name,
                  style: theme.textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.w600)),
              const SizedBox(height: 6),
              Row(
                children: [
                  const Icon(Icons.calendar_today_outlined, size: 16),
                  const SizedBox(width: 6),
                  Text('${dateFormat.format(trip.startDate)} – '
                      '${dateFormat.format(trip.endDate)} '
                      '(${trip.dayCount} gün)'),
                ],
              ),
              const SizedBox(height: 4),
              Row(
                children: [
                  const Icon(Icons.group_outlined, size: 16),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      '${trip.memberCount} üye · Kurucu: ${trip.ownerName}',
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (trip.myRole != null)
                    Text(roleLabelOf(trip.myRole), style: theme.textTheme.labelMedium),
                ],
              ),
              if (trip.description != null && trip.description!.isNotEmpty) ...[
                const SizedBox(height: 8),
                Text(
                  trip.description!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.bodySmall,
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off_outlined, size: 64),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh),
              label: const Text('Tekrar dene'),
            ),
          ],
        ),
      ),
    );
  }
}
