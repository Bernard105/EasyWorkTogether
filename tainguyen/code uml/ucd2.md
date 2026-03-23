@startuml
left to right direction
actor "User" as user

rectangle "Phân hệ Users" {
  usecase "U-102\nĐăng nhập hệ thống" as UC102
  usecase "Xác thực thông tin đăng nhập" as Authenticate
  usecase "Hiển thị captcha" as Captcha
  UC102 ..> Authenticate : <<include>>
  UC102 <.. Captcha : <<extend>>
}
user --> UC102
@enduml