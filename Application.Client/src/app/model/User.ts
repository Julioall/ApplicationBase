import { UserAccount } from "./user-account";
import { UserProfile } from "./user-profile";

export interface User {
    Id?: string;
    Account?: UserAccount;
    Profile?: UserProfile;
}