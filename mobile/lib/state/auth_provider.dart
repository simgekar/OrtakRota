import 'package:flutter/foundation.dart';

import '../models/app_user.dart';
import '../services/auth_service.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

/// Oturum durumunu tutar; değiştiğinde dinleyen ekranlara ve router'a haber verir.
class AuthProvider extends ChangeNotifier {
  AuthProvider(this._authService);

  final AuthService _authService;

  AuthStatus status = AuthStatus.unknown;
  AppUser? user;

  bool get isLoggedIn => status == AuthStatus.authenticated;

  Future<void> init() async {
    user = await _authService.currentUser();
    status = user != null ? AuthStatus.authenticated : AuthStatus.unauthenticated;
    notifyListeners();
  }

  Future<void> login(String email, String password) async {
    user = await _authService.login(email, password);
    status = AuthStatus.authenticated;
    notifyListeners();
  }

  Future<void> register(String fullName, String email, String password) async {
    user = await _authService.register(fullName, email, password);
    status = AuthStatus.authenticated;
    notifyListeners();
  }

  Future<void> logout() async {
    await _authService.logout();
    user = null;
    status = AuthStatus.unauthenticated;
    notifyListeners();
  }
}
