import { Box, Button, Checkbox, FormControl, InputLabel, ListItemText, MenuItem, Modal, OutlinedInput, Select, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import DeleteRoleDialog from './DeleteRoleDialog';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import LoadingComponent from '~/components/LoadingComponent';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 600,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4
};

const ITEM_HEIGHT = 48;
const ITEM_PADDING_TOP = 8;
const MenuProps = {
    PaperProps: {
        style: {
            maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
            width: 250,
        }
    }
};
function UserRoleModal({ currentUser, handleCloseMenu }) {
    const [open, setOpen] = useState(false);
    const [roles, setRoles] = useState([]);
    const [loading, setLoading] = useState(false);
    const [selectedRoles, setSelectedRoles] = useState([]);
    const [userRoles, setUserRoles] = useState([]);
    useEffect(() => {
        if (open) {
            getRoles();
        }
    }, [open])
    const getRoles = async () => {
        try {
            setLoading(true);
            await Promise.all([
                services.UserManagementAPI.getUserRoles(currentUser.id, (res) => {
                    setUserRoles(res.result);
                }, (err) => {
                    console.log(err);
                }),
                services.RoleManagementAPI.getRoles((res) => {
                    const filteredRoles = res.result.filter((role) => {
                        return !currentUser.role.includes(role.name)
                    })
                    setRoles(filteredRoles);
                }, (err) => {
                    console.log(err);
                })
            ])
            setLoading(false);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }
    const handleOpen = () => {
        setOpen(true)
    };
    const handleClose = () => {
        handleCloseMenu()
        setOpen(false);
    }
    const handleChange = (event) => {
        const {
            target: { value },
        } = event;
        setSelectedRoles(
            typeof value === 'string' ? value.split(',') : value,
        );
    };

    const handleSubmit = async () => {
        try {
            setLoading(true);
            const submitRoles = selectedRoles.map((role) => {
                const selectedRole = roles.find((r) => r.name === role);
                return selectedRole.id;
            })
            console.log(submitRoles);
            await services.RoleManagementAPI.assignRoles(currentUser.id, {
                userId: currentUser.id,
                userRoleIds: submitRoles
            }, (res) => {
                console.log(res);
                enqueueSnackbar("Assign role successfully!", { variant: "success" });
            }, (error) => {
                enqueueSnackbar("Assign role failed!", { variant: "error" });
            })
            setLoading(false);
        } catch (error) {
            console.log(error);
        }
    }
    const handleRemoveRole = async (id) => {
        try {
            setLoading(true);
            if (id) {
                await services.UserManagementAPI.removeUserRoles(currentUser.id, {
                    userId: currentUser.id,
                    userRoleIds: [id]
                }, (res) => {
                    enqueueSnackbar("Remove role successfully!", { variant: "success" });
                }, (error) => {
                    enqueueSnackbar("Remove role failed!", { variant: "error" });
                })
                setLoading(false);
                const filteredRoles = userRoles.filter((u) => {
                    return u.id !== id;
                })
                setUserRoles(filteredRoles);
            }
            else {
                enqueueSnackbar("Remove role failed!", { variant: "error" });
            }
            setLoading(false);
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <div>
            <MenuItem onClick={handleOpen}>Manage Role</MenuItem>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style} >
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Roles of {currentUser?.fullName}
                    </Typography>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }} mt="20px">
                        <FormControl size="small" sx={{ width: "80%" }}>
                            <InputLabel id="roles">Roles</InputLabel>
                            <Select
                                labelId="roles"
                                id="multiple-roles"
                                multiple
                                value={selectedRoles}
                                onChange={handleChange}
                                input={<OutlinedInput label="Roles" />}
                                renderValue={(selected) => selected.join(', ')}
                                MenuProps={MenuProps}
                            >
                                {roles?.map((role) => (
                                    <MenuItem key={role.id} value={role.name}>
                                        <Checkbox checked={selectedRoles.indexOf(role.name) > -1} />
                                        <ListItemText primary={role.name} />
                                    </MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                        <Button variant='contained' sx={{ alignSelf: "end", height: "40px", fontSize: "11px" }} onClick={handleSubmit}>Add Role</Button>
                    </Box>
                    <Box mt="20px">
                        {
                            userRoles?.map((l) => {
                                return (
                                    <Box key={l?.id} sx={{
                                        display: "flex", alignItems: "center", justifyContent: "space-between",
                                        height: "60px",
                                        '&:hover': {
                                            bgcolor: "#f7f7f9"
                                        },
                                        px: "20px",
                                        py: "10px",
                                    }}>
                                        <Box>
                                            {l?.name}
                                        </Box>
                                        <DeleteRoleDialog handleRemoveRole={handleRemoveRole} id={l?.id} />
                                    </Box>
                                )
                            })
                        }
                    </Box>
                    <LoadingComponent open={loading} setLoading={setLoading} />
                </Box>
            </Modal>
        </div>
    )
}

export default UserRoleModal
