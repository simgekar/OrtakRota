class TripMember {
  final String userId;
  final String fullName;
  final String role;

  const TripMember({
    required this.userId,
    required this.fullName,
    required this.role,
  });

  factory TripMember.fromJson(Map<String, dynamic> json) {
    return TripMember(
      userId: json['userId'] as String,
      fullName: json['fullName'] as String,
      role: json['role'] as String,
    );
  }

  String get roleLabel {
    switch (role) {
      case 'Owner':
        return 'Kurucu';
      case 'Editor':
        return 'Düzenleyici';
      default:
        return 'İzleyici';
    }
  }
}

class Trip {
  final int id;
  final String name;
  final String? description;
  final DateTime startDate;
  final DateTime endDate;
  final String inviteCode;
  final String ownerName;
  final int memberCount;

  // Yalnızca detay isteğinde (GET /api/trips/{id}) dolu gelir
  final List<TripMember> members;
  final int placeCount;
  final int expenseCount;

  const Trip({
    required this.id,
    required this.name,
    this.description,
    required this.startDate,
    required this.endDate,
    required this.inviteCode,
    required this.ownerName,
    required this.memberCount,
    this.members = const [],
    this.placeCount = 0,
    this.expenseCount = 0,
  });

  factory Trip.fromJson(Map<String, dynamic> json) {
    final members = (json['members'] as List<dynamic>? ?? [])
        .map((m) => TripMember.fromJson(m as Map<String, dynamic>))
        .toList();

    return Trip(
      id: json['id'] as int,
      name: json['name'] as String,
      description: json['description'] as String?,
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: DateTime.parse(json['endDate'] as String),
      inviteCode: json['inviteCode'] as String,
      ownerName: json['ownerName'] as String,
      memberCount: json['memberCount'] as int,
      members: members,
      placeCount: json['placeCount'] as int? ?? 0,
      expenseCount: json['expenseCount'] as int? ?? 0,
    );
  }

  int get dayCount => endDate.difference(startDate).inDays + 1;
}
