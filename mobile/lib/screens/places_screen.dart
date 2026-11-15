import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:go_router/go_router.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';

import '../models/place.dart';
import '../models/trip.dart';
import '../services/place_service.dart';
import '../services/trip_service.dart';
import '../state/auth_provider.dart';
import '../widgets/osm_map.dart';

/// Fikir havuzu: gezi için önerilen mekanlar, oylar ve harita görünümü
class PlacesScreen extends StatefulWidget {
  const PlacesScreen({super.key, required this.tripId});

  final int tripId;

  @override
  State<PlacesScreen> createState() => _PlacesScreenState();
}

class _PlacesScreenState extends State<PlacesScreen> {
  Trip? _trip;
  List<Place> _places = [];
  String? _category; // null: tüm kategoriler
  bool _showMap = false;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final tripService = context.read<TripService>();
    final placeService = context.read<PlaceService>();
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final results = await Future.wait([
        tripService.getTrip(widget.tripId),
        placeService.getPlaces(widget.tripId, category: _category),
      ]);
      if (!mounted) return;
      setState(() {
        _trip = results[0] as Trip;
        _places = results[1] as List<Place>;
      });
    } catch (error) {
      if (mounted) setState(() => _error = TripService.errorMessage(error));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _selectCategory(String? category) {
    setState(() => _category = category);
    _load();
  }

  /// Oy verir ya da geri alır; dönen güncel mekanla listeyi günceller ve yeniden sıralar
  Future<void> _vote(Place place, int value) async {
    final service = context.read<PlaceService>();
    final messenger = ScaffoldMessenger.of(context);
    try {
      final updated = await service.toggleVote(place, value);
      if (!mounted) return;
      setState(() => _replace(updated));
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
    }
  }

  void _replace(Place updated) {
    _places = [
      for (final p in _places) p.id == updated.id ? updated : p,
    ]..sort((a, b) {
        final byScore = b.score.compareTo(a.score);
        return byScore != 0 ? byScore : a.name.compareTo(b.name);
      });
  }

  Future<void> _addPlace() async {
    final created = await context.push<bool>('/trips/${widget.tripId}/places/new');
    if (created == true && mounted) _load();
  }

  Future<void> _openPlace(Place place) async {
    final action = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _PlaceSheet(
        place: place,
        placeService: context.read<PlaceService>(),
      ),
    );
    if (!mounted) return;
    // Pencere kapanınca düzenleme istendiyse form açılır; her durumda liste yenilenir
    // (pencerede oy verilmiş ya da mekan silinmiş olabilir)
    if (action == 'edit') {
      await context.push<bool>('/trips/${widget.tripId}/places/${place.id}/edit');
    }
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    final isAdmin = context.watch<AuthProvider>().user?.isAdmin ?? false;
    final canAdd = _trip != null && (_trip!.canEdit || isAdmin);

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Fikir Havuzu'),
            if (_trip != null)
              Text(_trip!.name, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
        actions: [
          IconButton(
            tooltip: _showMap ? 'Liste' : 'Harita',
            icon: Icon(_showMap ? Icons.view_list_outlined : Icons.map_outlined),
            onPressed: () => setState(() => _showMap = !_showMap),
          ),
        ],
      ),
      floatingActionButton: canAdd
          ? FloatingActionButton.extended(
              onPressed: _addPlace,
              icon: const Icon(Icons.add_location_alt_outlined),
              label: const Text('Mekan Öner'),
            )
          : null,
      body: Column(
        children: [
          _CategoryFilter(selected: _category, onSelected: _selectCategory),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _trip == null) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 12),
              OutlinedButton(onPressed: _load, child: const Text('Tekrar dene')),
            ],
          ),
        ),
      );
    }
    if (_showMap) {
      return _PlacesMap(places: _places, onPlaceTap: _openPlace);
    }
    if (_places.isEmpty) {
      return RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          children: const [
            SizedBox(height: 100),
            Icon(Icons.lightbulb_outline, size: 64),
            SizedBox(height: 12),
            Text('Henüz mekan önerisi yok.', textAlign: TextAlign.center),
            SizedBox(height: 4),
            Text('"Mekan Öner" ile ilk fikri sen ekle.', textAlign: TextAlign.center),
          ],
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        padding: const EdgeInsets.fromLTRB(12, 4, 12, 88),
        itemCount: _places.length,
        itemBuilder: (context, index) {
          final place = _places[index];
          return _PlaceCard(
            place: place,
            onTap: () => _openPlace(place),
            onVote: (value) => _vote(place, value),
          );
        },
      ),
    );
  }
}

class _CategoryFilter extends StatelessWidget {
  const _CategoryFilter({required this.selected, required this.onSelected});

  final String? selected;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 56,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        children: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ChoiceChip(
              label: const Text('Tümü'),
              selected: selected == null,
              onSelected: (_) => onSelected(null),
            ),
          ),
          for (final category in PlaceCategoryInfo.all)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ChoiceChip(
                avatar: Icon(category.icon, size: 18, color: category.color),
                label: Text(category.label),
                selected: selected == category.value,
                onSelected: (_) => onSelected(category.value),
              ),
            ),
        ],
      ),
    );
  }
}

class _PlaceCard extends StatelessWidget {
  const _PlaceCard({required this.place, required this.onTap, required this.onVote});

  final Place place;
  final VoidCallback onTap;
  final ValueChanged<int> onVote;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final info = place.categoryInfo;

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 4, 12),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: info.color.withValues(alpha: 0.15),
                child: Icon(info.icon, color: info.color),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(place.name,
                        style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
                    const SizedBox(height: 2),
                    Text('${info.label} · Öneren: ${place.addedByName}',
                        style: theme.textTheme.bodySmall),
                    if (place.note != null && place.note!.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(place.note!, maxLines: 2, overflow: TextOverflow.ellipsis),
                    ],
                  ],
                ),
              ),
              _VoteControls(place: place, onVote: onVote),
            ],
          ),
        ),
      ),
    );
  }
}

/// Beğen / puan / beğenme butonları. Kullanıcının oyu renkli gösterilir.
class _VoteControls extends StatelessWidget {
  const _VoteControls({required this.place, required this.onVote});

  final Place place;
  final ValueChanged<int> onVote;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          tooltip: 'Beğen',
          visualDensity: VisualDensity.compact,
          icon: Icon(place.myVote == 1 ? Icons.thumb_up : Icons.thumb_up_outlined,
              color: place.myVote == 1 ? colors.primary : null),
          onPressed: () => onVote(1),
        ),
        Text('${place.score}',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
        IconButton(
          tooltip: 'Beğenme',
          visualDensity: VisualDensity.compact,
          icon: Icon(place.myVote == -1 ? Icons.thumb_down : Icons.thumb_down_outlined,
              color: place.myVote == -1 ? colors.error : null),
          onPressed: () => onVote(-1),
        ),
      ],
    );
  }
}

class _PlacesMap extends StatelessWidget {
  const _PlacesMap({required this.places, required this.onPlaceTap});

  final List<Place> places;
  final ValueChanged<Place> onPlaceTap;

  @override
  Widget build(BuildContext context) {
    final points = [for (final p in places) LatLng(p.latitude, p.longitude)];

    return FlutterMap(
      options: MapOptions(
        initialCenter: turkeyCenter,
        initialZoom: 5.5,
        // Mekan varsa harita hepsini gösterecek şekilde açılır
        initialCameraFit: points.isEmpty
            ? null
            : CameraFit.coordinates(
                coordinates: points,
                padding: const EdgeInsets.all(56),
                maxZoom: 15,
              ),
      ),
      children: [
        osmTileLayer(),
        MarkerLayer(
          markers: [
            for (final place in places) placeMarker(place, onTap: () => onPlaceTap(place)),
          ],
        ),
        osmAttribution(),
      ],
    );
  }
}

/// Mekana dokununca açılan alt pencere: ayrıntılar, oylama, düzenleme ve silme
class _PlaceSheet extends StatefulWidget {
  const _PlaceSheet({required this.place, required this.placeService});

  final Place place;
  final PlaceService placeService;

  @override
  State<_PlaceSheet> createState() => _PlaceSheetState();
}

class _PlaceSheetState extends State<_PlaceSheet> {
  late Place _place = widget.place;

  Future<void> _vote(int value) async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      final updated = await widget.placeService.toggleVote(_place, value);
      if (!mounted) return;
      setState(() => _place = updated);
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
    }
  }


  Future<void> _delete() async {
    final navigator = Navigator.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Mekanı sil'),
        content: Text('"${_place.name}" fikir havuzundan silinsin mi?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Sil'),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await widget.placeService.deletePlace(_place.id);
      messenger.showSnackBar(const SnackBar(content: Text('Mekan silindi.')));
      navigator.pop('deleted');
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final info = _place.categoryInfo;

    return SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(info.icon, color: info.color),
                  const SizedBox(width: 8),
                  Text(info.label, style: theme.textTheme.labelLarge),
                ],
              ),
              const SizedBox(height: 8),
              Text(_place.name,
                  style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
              if (_place.address != null && _place.address!.isNotEmpty) ...[
                const SizedBox(height: 4),
                Text(_place.address!),
              ],
              if (_place.note != null && _place.note!.isNotEmpty) ...[
                const SizedBox(height: 12),
                Text(_place.note!),
              ],
              const SizedBox(height: 12),
              Text('Öneren: ${_place.addedByName}', style: theme.textTheme.bodySmall),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: _place.myVote == 1
                        ? FilledButton.icon(
                            onPressed: () => _vote(1),
                            icon: const Icon(Icons.thumb_up),
                            label: Text('${_place.upVotes}'),
                          )
                        : OutlinedButton.icon(
                            onPressed: () => _vote(1),
                            icon: const Icon(Icons.thumb_up_outlined),
                            label: Text('${_place.upVotes}'),
                          ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: Column(
                      children: [
                        Text('${_place.score}',
                            style: theme.textTheme.headlineSmall
                                ?.copyWith(fontWeight: FontWeight.bold)),
                        const Text('puan'),
                      ],
                    ),
                  ),
                  Expanded(
                    child: _place.myVote == -1
                        ? FilledButton.icon(
                            style: FilledButton.styleFrom(
                                backgroundColor: theme.colorScheme.error),
                            onPressed: () => _vote(-1),
                            icon: const Icon(Icons.thumb_down),
                            label: Text('${_place.downVotes}'),
                          )
                        : OutlinedButton.icon(
                            onPressed: () => _vote(-1),
                            icon: const Icon(Icons.thumb_down_outlined),
                            label: Text('${_place.downVotes}'),
                          ),
                  ),
                ],
              ),
              if (_place.canEdit) ...[
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => Navigator.of(context).pop('edit'),
                        icon: const Icon(Icons.edit_outlined),
                        label: const Text('Düzenle'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: _delete,
                        icon: const Icon(Icons.delete_outline),
                        label: const Text('Sil'),
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
    );
  }
}
