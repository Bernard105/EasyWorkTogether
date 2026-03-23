@startuml
left to right direction
actor "User" as user

rectangle "Phân hệ Users" {
  usecase "U-103\nĐăng xuất hệ thống" as UC103
}
user --> UC103
@enduml