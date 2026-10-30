/// Formlarda kullanılan doğrulama kuralları (API'deki kurallarla aynı).
class Validators {
  static final _emailRegex = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  static String? email(String? value) {
    if (value == null || value.trim().isEmpty) return 'E-posta zorunludur.';
    if (!_emailRegex.hasMatch(value.trim())) return 'Geçerli bir e-posta adresi girin.';
    return null;
  }

  static String? loginPassword(String? value) {
    if (value == null || value.isEmpty) return 'Şifre zorunludur.';
    return null;
  }

  static String? fullName(String? value) {
    if (value == null || value.trim().isEmpty) return 'Ad soyad zorunludur.';
    if (value.trim().length < 3) return 'Ad soyad en az 3 karakter olmalıdır.';
    return null;
  }

  /// Kayıtta şifre kuralı: en az 6 karakter, bir büyük harf, bir küçük harf, bir rakam.
  static String? newPassword(String? value) {
    if (value == null || value.isEmpty) return 'Şifre zorunludur.';
    if (value.length < 6) return 'Şifre en az 6 karakter olmalıdır.';
    if (!value.contains(RegExp(r'[A-Z]'))) return 'Şifre en az bir büyük harf içermelidir.';
    if (!value.contains(RegExp(r'[a-z]'))) return 'Şifre en az bir küçük harf içermelidir.';
    if (!value.contains(RegExp(r'[0-9]'))) return 'Şifre en az bir rakam içermelidir.';
    return null;
  }
}
