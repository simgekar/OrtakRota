class AppUser {
  final String id;
  final String fullName;
  final String email;
  final List<String> roles;

  const AppUser({
    required this.id,
    required this.fullName,
    required this.email,
    required this.roles,
  });

  factory AppUser.fromJson(Map<String, dynamic> json) {
    return AppUser(
      id: json['id'] as String,
      fullName: json['fullName'] as String,
      email: json['email'] as String,
      roles: (json['roles'] as List<dynamic>? ?? [])
          .map((role) => role as String)
          .toList(),
    );
  }

  bool get isAdmin => roles.contains('Admin');
}
