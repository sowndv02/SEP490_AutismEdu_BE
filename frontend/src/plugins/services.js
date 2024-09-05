import axios from "~/plugins/axios";
import { initializeService } from "~/services/BaseService";
import { AuthenticationAPI } from "~/services/AuthenticationAPI";
import { UserManagementAPI } from "~/services/UserManagementAPI";
import { ClaimManagementAPI } from "~/services/ClaimManagementAPI";

// Initialize the BaseService with the axios instance and API prefix
initializeService(axios, '/api');

const services = {
  AuthenticationAPI,
  UserManagementAPI,
  ClaimManagementAPI
};

export default services;