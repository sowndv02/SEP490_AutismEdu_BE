<<<<<<< HEAD
import { Box, Button, FormControl, InputLabel, MenuItem, Modal, Select, Typography } from '@mui/material';
import { useState } from 'react';
=======
import { Box, Button, Checkbox, FormControl, InputLabel, ListItemText, MenuItem, Modal, OutlinedInput, Select, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
import DeleteRoleDialog from './DeleteRoleDialog';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 600,
    bgcolor: 'background.paper',
    boxShadow: 24,
<<<<<<< HEAD
    p: 4,
};
function UserRoleModal({ handleCloseMenu }) {
    const [open, setOpen] = useState(false);
    const [claim, setClaim] = useState('');
    const listClaim = [
        "Admin",
        "Customer"
    ]

    const listMyClaim = [
        "Admin"
    ]
=======
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
                    const filteredRoles = res.result.filter((role) => {
                        return role.name !== "User"
                    })
                    setUserRoles(filteredRoles);
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
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const handleOpen = () => {
        setOpen(true)
    };
    const handleClose = () => {
        handleCloseMenu()
        setOpen(false);
    }
<<<<<<< HEAD

    const handleChange = (event) => {
        setClaim(event.target.value);
    };
=======
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
            if (selectedRoles.length !== 0) {
                setLoading(true);
                const submitRoles = selectedRoles.map((role) => {
                    const selectedRole = roles.find((r) => r.name === role);
                    return selectedRole.id;
                })
                await services.RoleManagementAPI.assignRoles(currentUser.id, {
                    userId: currentUser.id,
                    userRoleIds: submitRoles
                }, (res) => {
                    setUserRoles([...res.result, ...userRoles]);
                    const unAssignRoles = roles.filter((role) => {
                        return !submitRoles.includes(role.id);
                    })
                    setRoles(unAssignRoles)
                    enqueueSnackbar("Assign role successfully!", { variant: "success" });
                }, (error) => {
                    enqueueSnackbar("Assign role failed!", { variant: "error" });
                })
                setLoading(false);
                setSelectedRoles([])
            }
        } catch (error) {
            console.log(error);
        }
    }
    const handleRemoveRole = async (id) => {
        try {
            setLoading(true);
            if (id) {
                const selectedRole = userRoles.find((role) => role.id === id);
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
                setRoles([selectedRole, ...roles])
            }
            else {
                enqueueSnackbar("Remove role failed!", { variant: "error" });
            }
            setLoading(false);
        } catch (error) {
            console.log(error);
        }
    }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
<<<<<<< HEAD
                        Roles of Khai Dao
                    </Typography>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }} mt="20px">
                        <FormControl size="small" sx={{ width: "80%" }}>
                            <InputLabel id="demo-simple-select-label">Roles</InputLabel>
                            <Select
                                labelId="demo-simple-select-label"
                                id="demo-simple-select"
                                value={claim}
                                label="Claim"
                                onChange={handleChange}
                            >
                                {listClaim.map((l, index) => {
                                    return (
                                        <MenuItem key={index} value={10}>{l}</MenuItem>
                                    )
                                })}
                            </Select>
                        </FormControl>
                        <Button variant='contained' sx={{ alignSelf: "end", height: "40px", fontSize: "11px" }}>Add Role</Button>
                    </Box>
                    <Box mt="20px">
                        {
                            listMyClaim.map((l, index) => {
                                return (
                                    <Box key={index} sx={{
=======
                        Roles of {currentUser?.fullName}
                    </Typography>
                    {
                        roles.length !== 0 && (
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
                                        {
                                            roles?.map((role) => (
                                                <MenuItem key={role.id} value={role.name}>
                                                    <Checkbox checked={selectedRoles.indexOf(role.name) > -1} />
                                                    <ListItemText primary={role.name} />
                                                </MenuItem>
                                            ))
                                        }
                                    </Select>
                                </FormControl>
                                <Button variant='contained' sx={{ alignSelf: "end", height: "40px", fontSize: "11px" }} onClick={handleSubmit}>Add Role</Button>
                            </Box>
                        )
                    }
                    <Box mt="20px">
                        {
                            userRoles?.map((l) => {
                                return (
                                    <Box key={l?.id} sx={{
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                                        display: "flex", alignItems: "center", justifyContent: "space-between",
                                        height: "60px",
                                        '&:hover': {
                                            bgcolor: "#f7f7f9"
                                        },
                                        px: "20px",
                                        py: "10px",
                                    }}>
                                        <Box>
<<<<<<< HEAD
                                            {l}
                                        </Box>
                                        <DeleteRoleDialog />
=======
                                            {l?.name}
                                        </Box>
                                        <DeleteRoleDialog handleRemoveRole={handleRemoveRole} id={l?.id} />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                                    </Box>
                                )
                            })
                        }
                    </Box>
<<<<<<< HEAD
=======
                    <LoadingComponent open={loading} setLoading={setLoading} />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                </Box>
            </Modal>
        </div>
    )
}

export default UserRoleModal
