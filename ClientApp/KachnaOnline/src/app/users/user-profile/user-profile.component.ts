import { Component, OnInit } from '@angular/core';
import {AuthenticationService} from "../../shared/services/authentication.service";
import {NgForm} from "@angular/forms";

@Component({
  selector: 'app-user',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  public isCardInfoCollapsed: boolean = false;

  constructor(
    public authenticationService: AuthenticationService,
  ) { }

  gamificationConsentTooltipText:string = "Tohle je souhlas s gamifikací."
  cardNumberTooltipText:string = "Tohle je krásná karta."

  ngOnInit(): void {
  }

  onSaveChanges(form: NgForm) {
    // TODO: Save changes to user nickname, card and gamification approval.
  }
}
