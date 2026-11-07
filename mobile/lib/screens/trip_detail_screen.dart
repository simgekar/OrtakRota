import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../models/trip.dart';
import '../services/trip_service.dart';
import '../state/auth_provider.dart';

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

  void _reload() {
    if (!mounted) return;
    setState(() {
      _tripFuture = context.read<TripService>().getTrip(widget.tripId);
    });
  }

  /// Onay penceresi gösterir; kullanıcı onaylarsa true döner.
  Future<bool> _confirm(String title, String message, String action) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(action),
          ),
        ],
      ),
    );
    return result ?? false;
  }

  /// API çağrısını yapar, sonucu SnackBar ile bildirir.
  Future<bool> _run(Future<void> Function() action, String successMessage) async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      await action();
      messenger.showSnackBar(SnackBar(content: Text(successMessage)));
      return true;
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
      return false;
    }
  }

  Future<void> _edit() async {
    final changed = await context.push<bool>('/trips/${widget.tripId}/edit');
    if (changed == true) _reload();
  }

  Future<void> _delete(Trip trip) async {
    final service = context.read<TripService>();
    final router = GoRouter.of(context);
    final ok = await _confirm(
      'Geziyi sil',
      '"${trip.name}" gezisi ve içindeki tüm mekan, plan ve harcamalar silinecek. Emin misin?',
      'Sil',
    );
    if (!ok) return;
    if (await _run(() => service.deleteTrip(trip.id), 'Gezi silindi.')) router.pop(true);
  }

  Future<void> _leave(Trip trip, String myUserId) async {
    final service = context.read<TripService>();
    final router = GoRouter.of(context);
    final ok = await _confirm('Geziden ayrıl', '"${trip.name}" gezisinden ayrılmak istiyor musun?', 'Ayrıl');
    if (!ok) return;
    if (await _run(() => service.removeMember(trip.id, myUserId), 'Geziden ayrıldın.')) {
      router.pop(true);
    }
  }

  Future<void> _regenerateCode(Trip trip) async {
    final service = context.read<TripService>();
    final ok = await _confirm(
      'Davet kodunu yenile',
      'Eski kod geçersiz olacak; yeni gelecek kişilere yeni kodu paylaşman gerekecek.',
      'Yenile',
    );
    if (!ok) return;
    if (await _run(() => service.regenerateInviteCode(trip.id), 'Yeni davet kodu oluşturuldu.')) {
      _reload();
    }
  }

  Future<void> _memberAction(Trip trip, TripMember member, String action) async {
    final service = context.read<TripService>();
    bool done;
    if (action == 'remove') {
      final ok = await _confirm(
          'Üyeyi çıkar', '${member.fullName} geziden çıkarılsın mı?', 'Çıkar');
      if (!ok) return;
      done = await _run(() => service.removeMember(trip.id, member.userId),
          '${member.fullName} geziden çıkarıldı.');
    } else {
      done = await _run(() => service.updateMemberRole(trip.id, member.userId, action),
          '${member.fullName} artık ${roleLabelOf(action)}.');
    }
    if (done) _reload();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final isAdmin = user?.isAdmin ?? false;

    return FutureBuilder<Trip>(
      future: _tripFuture,
      builder: (context, snapshot) {
        final trip = snapshot.data;
        // Admin tüm gezilerde kurucu yetkisine sahiptir
        final canEdit = trip != null && (trip.canEdit || isAdmin);
        final canManage = trip != null && (trip.isOwner || isAdmin);

        return Scaffold(
          appBar: AppBar(
            title: const Text('Gezi Detayı'),
            actions: [
              if (canEdit)
                IconButton(
                  tooltip: 'Düzenle',
                  icon: const Icon(Icons.edit_outlined),
                  onPressed: _edit,
                ),
              if (canManage)
                IconButton(
                  tooltip: 'Sil',
                  icon: const Icon(Icons.delete_outline),
                  onPressed: () => _delete(trip),
                ),
            ],
          ),
          body: () {
            if (snapshot.connectionState == ConnectionState.waiting && trip == null) {
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
            final loaded = trip!;
            return _TripDetailBody(
              trip: loaded,
              myUserId: user?.id,
              canManage: canManage,
              onRefresh: () async => _reload(),
              onCopyCode: () {
                Clipboard.setData(ClipboardData(text: loaded.inviteCode));
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Davet kodu kopyalandı.')),
                );
              },
              onRegenerateCode: () => _regenerateCode(loaded),
              onMemberAction: (member, action) => _memberAction(loaded, member, action),
              onLeave: loaded.myRole != null && !loaded.isOwner && user != null
                  ? () => _leave(loaded, user.id)
                  : null,
            );
          }(),
        );
      },
    );
  }
}

class _TripDetailBody extends StatelessWidget {
  const _TripDetailBody({
    required this.trip,
    required this.myUserId,
    required this.canManage,
    required this.onRefresh,
    required this.onCopyCode,
    required this.onRegenerateCode,
    required this.onMemberAction,
    required this.onLeave,
  });

  final Trip trip;
  final String? myUserId;
  final bool canManage;
  final Future<void> Function() onRefresh;
  final VoidCallback onCopyCode;
  final VoidCallback onRegenerateCode;
  final void Function(TripMember member, String action) onMemberAction;
  final VoidCallback? onLeave;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dateFormat = DateFormat('dd.MM.yyyy');

    return RefreshIndicator(
      onRefresh: onRefresh,
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 700),
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(trip.name,
                  style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
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
                    avatar: const Icon(Icons.badge_outlined, size: 18),
                    label: Text('Rolün: ${roleLabelOf(trip.myRole)}'),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Card(
                child: ListTile(
                  leading: const Icon(Icons.key_outlined),
                  title: const Text('Davet kodu'),
                  subtitle: Text(trip.inviteCode,
                      style: theme.textTheme.titleLarge?.copyWith(letterSpacing: 4)),
                  trailing: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      IconButton(
                        tooltip: 'Kopyala',
                        icon: const Icon(Icons.copy),
                        onPressed: onCopyCode,
                      ),
                      if (canManage)
                        IconButton(
                          tooltip: 'Yenile',
                          icon: const Icon(Icons.autorenew),
                          onPressed: onRegenerateCode,
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: _StatCard(
                        icon: Icons.place_outlined, label: 'Mekan', value: trip.placeCount),
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
              Text('Üyeler (${trip.members.length})', style: theme.textTheme.titleMedium),
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
                        title: Text(member.userId == myUserId
                            ? '${member.fullName} (sen)'
                            : member.fullName),
                        subtitle: Text(member.roleLabel),
                        // Kurucu, diğer üyelerin rolünü değiştirebilir veya onları çıkarabilir
                        trailing: canManage && !member.isOwner
                            ? PopupMenuButton<String>(
                                onSelected: (action) => onMemberAction(member, action),
                                itemBuilder: (context) => [
                                  if (member.role != 'Editor')
                                    const PopupMenuItem(
                                        value: 'Editor', child: Text('Düzenleyici yap')),
                                  if (member.role != 'Viewer')
                                    const PopupMenuItem(
                                        value: 'Viewer', child: Text('İzleyici yap')),
                                  const PopupMenuItem(
                                      value: 'remove', child: Text('Geziden çıkar')),
                                ],
                              )
                            : null,
                      ),
                  ],
                ),
              ),
              if (onLeave != null) ...[
                const SizedBox(height: 16),
                OutlinedButton.icon(
                  onPressed: onLeave,
                  icon: const Icon(Icons.logout),
                  label: const Text('Geziden ayrıl'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({required this.icon, required this.label, required this.value});

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
