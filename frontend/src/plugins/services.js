import axios from "~/plugins/axios";
import { initializeService } from "~/services/BaseService";
import { AuthenticationAPI } from "~/services/AuthenticationAPI";
<<<<<<< HEAD
=======
import { UserManagementAPI } from "~/services/UserManagementAPI";
import { ClaimManagementAPI } from "~/services/ClaimManagementAPI";
import { RoleManagementAPI } from "~/services/RoleManagementAPI";
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f

// Initialize the BaseService with the axios instance and API prefix
(function() {
  initializeService(axios, "/api");
})();

const services = {
  AuthenticationAPI,
<<<<<<< HEAD
=======
  UserManagementAPI,
  ClaimManagementAPI,
  RoleManagementAPI
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
};

export default services;
