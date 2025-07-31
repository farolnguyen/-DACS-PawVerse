import bcrypt

# Mật khẩu cần mã hóa
password = "Admin123"

# Mã hóa mật khẩu
hashed_password = bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt())

# In mật khẩu đã mã hóa ra console
print("Hashed Password: ")
print(hashed_password.decode('utf-8'))
