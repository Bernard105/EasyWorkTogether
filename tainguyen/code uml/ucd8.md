@startuml
left to right direction
actor "User (người mời)" as inviter
actor "Invited Member" as invited
actor "Email Service" as email

rectangle "Phân hệ Workspace" {
  usecase "W-203\nMời và quản lý thành viên" as UC203
  usecase "Gửi email mời" as SendInvite
  usecase "Gửi lại lời mời" as ResendInvite
  UC203 ..> SendInvite : <<include>>
  UC203 <.. ResendInvite : <<extend>>
}
inviter --> UC203
invited --> UC203
email <-- SendInvite
email <-- ResendInvite
@enduml