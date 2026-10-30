import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../models/trip.dart';
import '../services/trip_service.dart';

class TripDetailScreen extends StatefulWidget {
  const TripDetailScreen({super.key, required this.tripId});

  final int tripId;

  @override
  State<TripDetailScreen> createState() => _TripDetailScreenState();
}

class _TripDetailScreenState extends State<TripDetailScreen> {
  late Future<Trip> _tripFuture;

  @override
  void initState() {
    super.initState();
    _tripFuture = context.read<TripService>().getTrip(widget.tripId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Gezi Detayı')),
      body: FutureBuilder<Trip>(
        future: _tripFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(TripService.errorMessage(snapshot.error!),
                    textAlign: TextAlign.center),
              ),
            );
          }
          return _TripDetailBody(trip: snapshot.data!);
        },
      ),
    );
  }
}

class _TripDetailBody extends StatelessWidget {
  const _TripDetailBody({required this.trip});

  final Trip trip;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dateFormat = DateFormat('dd.MM.yyyy');

    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 700),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(trip.name,
                style: theme.textTheme.headlineSmall
                    ?.copyWith(fontWeight: FontWeight.bold)),
            if (trip.description != null && trip.description!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(trip.description!),
            ],
            const SizedBox(height: 16),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                Chip(
                  avatar: const Icon(Icons.calendar_today_outlined, size: 18),
                  label: Text('${dateFormat.format(trip.startDate)} – '
                      '${dateFormat.format(trip.endDate)}'),
                ),
                Chip(
                  avatar: const Icon(Icons.timelapse, size: 18),
                  label: Text('${trip.dayCount} gün'),
                ),
                Chip(
                  avatar: const Icon(Icons.key_outlined, size: 18),
                  label: Text('Davet kodu: ${trip.inviteCode}'),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _StatCard(
                      icon: Icons.place_outlined,
                      label: 'Mekan',
                      value: trip.placeCount),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _StatCard(
                      icon: Icons.receipt_long_outlined,
                      label: 'Harcama',
                      value: trip.expenseCount),
                ),
              ],
            ),
            const SizedBox(height: 24),
            Text('Üyeler (${trip.members.length})',
                style: theme.textTheme.titleMedium),
            const SizedBox(height: 8),
            Card(
              child: Column(
                children: [
                  for (final member in trip.members)
                    ListTile(
                      leading: CircleAvatar(
                        child: Text(member.fullName.isNotEmpty
                            ? member.fullName[0].toUpperCase()
                            : '?'),
                      ),
                      title: Text(member.fullName),
                      trailing: Text(member.roleLabel),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard(
      {required this.icon, required this.label, required this.value});

  final IconData icon;
  final String label;
  final int value;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Icon(icon),
            const SizedBox(height: 4),
            Text('$value', style: Theme.of(context).textTheme.titleLarge),
            Text(label),
          ],
        ),
      ),
    );
  }
}
