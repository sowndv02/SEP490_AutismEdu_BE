import RemoveIcon from '@mui/icons-material/Remove';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import * as React from 'react';
<<<<<<< HEAD
function DeleteClaimDialog() {
=======
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import services from '~/plugins/services';
function DeleteClaimDialog({ setApiCall, numberClaim}) {
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const [open, setOpen] = React.useState(false);

    const handleClickOpen = () => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };
<<<<<<< HEAD
    return (
        <React.Fragment>
            <IconButton onClick={handleClickOpen}>
                <RemoveIcon sx={{ color: "#FF8343" }} />
=======

    const handleDelete = () => {
        setApiCall(2);
        handleClose();
    }
    return (
        <React.Fragment>
            <IconButton onClick={handleClickOpen}>
                <DeleteOutlineIcon />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
            </IconButton>
            <Dialog
                open={open}
                onClose={handleClose}
                aria-labelledby="alert-dialog-title"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title">
<<<<<<< HEAD
                    {"Do you want remove this claim"}
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleClose}>Delete</Button>
=======
                    {`Do you want remove ${numberClaim} claim`}
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleDelete}>Delete</Button>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    <Button onClick={handleClose} autoFocus>
                        Cancle
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    )
}

export default DeleteClaimDialog
