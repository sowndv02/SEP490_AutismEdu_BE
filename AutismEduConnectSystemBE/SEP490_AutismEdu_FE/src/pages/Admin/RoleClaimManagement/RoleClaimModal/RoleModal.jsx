import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Modal from '@mui/material/Modal';
import { useEffect, useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import { Divider, FormControl, FormHelperText, InputLabel, MenuItem, Select, TextField } from '@mui/material';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 400,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
};

function RoleModal({ roles, setRoles }) {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const [role, setRole] = useState('');
    useEffect(() => {
        if (!open) {
            setRole("")
        }
    }, [open]);

    const [isSaveDisabled, setIsSaveDisabled] = useState(true);
    const [errors, setErrors] = useState({});

    const validateForm = () => {


        const newErrors = {};


        if (role.length > 20) {
            newErrors.role = 'Không được vượt quá 20 ký tự';
        }
        if (!role) {
            newErrors.role = 'Không được để trống';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    useEffect(() => {
        setIsSaveDisabled(!validateForm());
    }, [role]);

    const handleAddRole = async () => {
        try {
            await services.RoleManagementAPI.addRole({ name: role }, (res) => {
                if (res?.result) {
                    setRoles((prev) => [res.result, ...prev]);
                    enqueueSnackbar("Thêm vai trò thành công!", { variant: "success" });
                    setOpen(false);
                }
            }
                , (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" });
                    console.log(error);
                    setOpen(false);
                });
        } catch (error) {
            console.log(error);
        }
    }

    return (
        <Box>
            <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={handleOpen}>
                Tạo vai trò
            </Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box
                    sx={{
                        ...style,
                        width: 400, 
                        margin: 'auto', 
                        p: 3,
                        display: 'flex',
                        flexDirection: 'column', 
                        gap: 1, 
                        bgcolor: 'background.paper',
                        boxShadow: 24,
                        borderRadius: 2, 
                    }}
                >
                    <Typography
                        id="modal-modal-title"
                        variant="h5"
                        textAlign="center"
                        sx={{ marginBottom: 0 }}
                    >
                        Tạo vai trò
                    </Typography>
                    <Divider />
                    <TextField
                        size="small"
                        id="outlined-basic"
                        label="Vai trò"
                        variant="outlined"
                        value={role}
                        onChange={(e) => setRole(e.target.value)}
                        sx={{ width: "100%", marginTop: 2 }}
                    />
                    {errors.role && (
                        <FormHelperText error>{errors.role}</FormHelperText>
                    )}
                    <Box
                        sx={{
                            display: 'flex',
                            justifyContent: 'flex-end', 
                            marginTop: 1,
                        }}
                    >
                        <Button
                            variant="contained"
                            onClick={handleAddRole}
                            disabled={isSaveDisabled}>
                            Tạo
                        </Button>
                    </Box>
                </Box>
            </Modal>
        </Box>
    );
}

export default RoleModal
