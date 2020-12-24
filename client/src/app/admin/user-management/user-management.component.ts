import { Component, OnInit } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { RolesModalComponent } from 'src/app/modals/roles-modal/roles-modal.component';
import { User } from 'src/app/_models/user';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css']
})
export class UserManagementComponent implements OnInit {
  users: Partial<User[]>;
  bsModalRef: BsModalRef;

  constructor(private adminSerive: AdminService, private modalService: BsModalService) { }

  ngOnInit(): void {
    this.getUsersWithRoles();
  }

  getUsersWithRoles() {
    this.adminSerive.getUsersWithRoles().subscribe(users => {
      this.users = users;
    });
  }

  openRolesModal(user: User) {
    const config = {
      class: 'modal-dialog-center',
      initialState: {
        user,
        roles: this.getRolesArray(user)
      }
    };

    this.bsModalRef = this.modalService.show(RolesModalComponent, config );
    //this.bsModalRef.content.closeBtnName = 'Close';
    this.bsModalRef.content.updateSelectedRoles.subscribe(values => {
      const rolesToUpdate = {
        roles: [...values.filter(el => el.checked === true).map(el => el.name)]
      };

      if(rolesToUpdate){
        this.adminSerive.updateUserRoles(user.userName, rolesToUpdate.roles).subscribe(() => {
          user.roles = [...rolesToUpdate.roles];
        })
      }
    });
  }

  private getRolesArray(user) {
    const roles = [];
    const userRoles = user.roles;

    const availableRoles: any[] = [
      { name: 'Admin', value: 'Admin' },
      { name: 'Moderator', value: 'Moderator' },
      { name: 'Member', value: 'Member' }
    ];

    availableRoles.forEach(role => {
      let isMatch = false;
      for (const userRole of userRoles) {
        if (userRole === role.name) {
          isMatch = true;
          role.checked = true;
          roles.push(role);
          break;
        }
      }

      if(!isMatch){
        role.checked = false;
        roles.push(role);
      }
    });

    return roles;
  }
}
