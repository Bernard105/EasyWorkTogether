@startuml
left to right direction
actor "User (admin/owner)" as user

rectangle "Phân hệ Workspace" {
  usecase "W-202\nQuản lý Workspace" as UC202
}
user --> UC202
@endumls