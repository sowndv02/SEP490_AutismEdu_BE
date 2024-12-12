import RemoveIcon from '@mui/icons-material/Remove';
import { Avatar, AvatarGroup, IconButton } from '@mui/material';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemAvatar from '@mui/material/ListItemAvatar';
import ListItemText from '@mui/material/ListItemText';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import * as React from 'react';
import { useEffect, useState } from 'react';
import DeleteRoleDialog from './DeleteRoleDialog';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import DeleteRoleSDialog from './DeleteRoleSDialog';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 450,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
};
//users={r.users} roles={roles} setRoles={setRoles} roleId={r.id} 
function ListUserModal({ totalUser, totalUsersInRole, claim, setClaim, users, claimId, roleId, roles, setRoles }) {

    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const handleDeleteUserFromClaim = (id) => {
        try {

            services.UserManagementAPI.removeUserClaims(id,
                {
                    userId: id,
                    userClaimIds: [claimId]
                }, (res) => {
                    enqueueSnackbar("Remove user from claim successfully!", { variant: "success" });
                    const claims = claim.map(c => {
                        if (c.id === claimId) {
                            c.users = users.filter((u) => (u.id !== id));
                            return c;
                        } else {
                            return c;
                        }
                    });
                    setClaim(claims);

                }, (err) => {
                    enqueueSnackbar("Remove user from claim failed!", { variant: "error" });
                    console.log(err);
                })
        } catch (error) {
            console.log(error);
        }
    };

    const handleDeleteUserFromRole = async (id) => {
        try {

            await services.UserManagementAPI.removeUserRoles(id,
                {
                    userId: id,
                    userRoleIds: [roleId]
                }, (res) => {
                    enqueueSnackbar("Remove user from role successfully!", { variant: "success" });
                    const role = roles.map(r => {
                        if (r.id === roleId) {
                            r.users = r.users.filter((u) => (u.id !== id));
                            return r;
                        } else {
                            return r;
                        }
                    });
                    setRoles(role);
                    
                }, (err) => {
                    enqueueSnackbar("Remove user from role failed!", { variant: "error" });
                    console.log(err);
                })
        } catch (error) {
            console.log(error);
        }
    };

    return (
        <div>
            <AvatarGroup max={4} total={totalUser || totalUsersInRole} sx={{
                "&.MuiAvatarGroup-root": {
                    justifyContent: "start"
                },
                cursor: "pointer"
            }} onClick={handleOpen}>
                {users?.map((u, index) => (
                    <Avatar alt={'?'} src={u.imageLocalUrl} />
                ))}

            </AvatarGroup>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Create - User Account
                    </Typography>
                    <Box mt="20px">
                        <List sx={{ width: '100%', maxWidth: 360, bgcolor: 'background.paper' }}>
                            {users?.map((u, index) => (
                                <>
                                    <ListItem alignItems="flex-start"
                                        secondaryAction={
                                            roleId ? (<DeleteRoleSDialog handleDeleteUserFromRole={handleDeleteUserFromRole} id={u.id} />) :
                                                <DeleteRoleDialog handleDeleteUserFromClaim={handleDeleteUserFromClaim} id={u.id} />
                                        }>
                                        <ListItemAvatar>
                                            <Avatar alt={'?'} src={u.imageLocalUrl} />
                                        </ListItemAvatar>
                                        <ListItemText
                                            primary={u.fullName || ''}
                                            secondary={
                                                <React.Fragment>
                                                    <Typography
                                                        sx={{ display: 'inline' }}
                                                        component="span"
                                                        variant="body2"
                                                        color="text.primary"
                                                    >
                                                        {u.email}
                                                    </Typography>
                                                </React.Fragment>
                                            }
                                        />
                                    </ListItem>
                                    <Divider variant="inset" component="li" />
                                </>
                            ))}

                        </List>
                    </Box>
                </Box>
            </Modal>
        </div>
    );
}

export default ListUserModal
