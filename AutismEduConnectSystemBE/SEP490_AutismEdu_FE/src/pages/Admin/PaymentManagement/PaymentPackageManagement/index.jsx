import { Box, Button, MenuItem, Paper, Select, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import ConfirmDialog from '~/components/ConfirmDialog';
import services from '~/plugins/services';
import PaymentCreation from './PaymentCreation';
import PaymentUpdate from './PaymentUpdater';
function PaymentPackageManagement() {
    const [paymentPackages, setPaymetPackages] = useState([]);
    const [change, setChange] = useState(true);
    const [openConfirm, setOpenConfirm] = useState(false);
    const [currentPackage, setCurrentPackage] = useState(null);
    const [status, setStatus] = useState("all");
    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
    useEffect(() => {
        getPaymentPackages()
    }, [])
    useEffect(() => {
        if (status !== "all") {
            setStatus("all")
        } else {
            getPaymentPackages();
        }
    }, [change])

    useEffect(() => {
        getPaymentPackages()
    }, [status])

    const getPaymentPackages = async () => {
        try {
            await services.PackagePaymentAPI.getListPaymentPackage((res) => {
                setPaymetPackages(res.result)
            }, (error) => {
                console.log(error);
            }, {
                status: status
            })
        } catch (error) {
            console.log(error);
        }
    }

    const formatter = new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    });

    const changeStatus = async () => {
        try {
            await services.PackagePaymentAPI.updatePaymentPackage(currentPackage.id, {
                id: currentPackage.id,
                isActive: currentPackage.isHide ? false : true
            },
                (res) => {
                    if (status !== "all") {
                        const filterUpdate = paymentPackages.filter((p) => {
                            return p.id !== currentPackage.id;
                        })
                        setPaymetPackages(filterUpdate);
                    }
                    else {
                        const filterUpdate = paymentPackages.map((p) => {
                            if (p.id !== currentPackage.id) {
                                return p;
                            } else return res.result;
                        })
                        setPaymetPackages(filterUpdate);
                    }
                    enqueueSnackbar("Cập nhật thành công", { variant: "success" })
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                })
            setOpenConfirm(false);
        } catch (error) {
            enqueueSnackbar("Cập nhật thất bại", { variant: "error" })
            setOpenConfirm(false);
        }
    }
    console.log(paymentPackages);
    return (
        <Paper variant='elevation' sx={{ p: 3 }}>
            <Typography variant='h4'>Quản lý gói thanh toán</Typography>
            <Box sx={{ display: "flex", justifyContent: "space-between" }} mt={5}>
                <Select value={status} onChange={(e) => setStatus(e.target.value)}>
                    <MenuItem value="all">Tất cả</MenuItem>
                    <MenuItem value="Show">Đang hiện</MenuItem>
                    <MenuItem value="Hide">Đang ẩn</MenuItem>
                </Select>
                <PaymentCreation change={change} setChange={setChange} />
            </Box>
            <TableContainer component={Paper} sx={{ mt: 5 }}>
                <Table sx={{ minWidth: 650 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>STT</TableCell>
                            <TableCell>Tên gói</TableCell>
                            <TableCell align="center">Thời gian (tháng)</TableCell>
                            <TableCell align="center">Giá</TableCell>
                            <TableCell>Ngày tạo</TableCell>
                            <TableCell>Người tao</TableCell>
                            <TableCell>Hành động</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {
                            paymentPackages.length !== 0 && paymentPackages.map((s, index) => {
                                return (
                                    <TableRow key={s.id}>
                                        <TableCell>
                                            {index + 1}
                                        </TableCell>
                                        <TableCell>
                                            {s.title}
                                        </TableCell>
                                        <TableCell align="center">
                                            {s.duration}
                                        </TableCell>
                                        <TableCell align="center">
                                            {formatter.format(s.price)}
                                        </TableCell>
                                        <TableCell>
                                            {formatDate(s.createdDate)}
                                        </TableCell>
                                        <TableCell>
                                            {s?.submitter?.email}
                                        </TableCell>
                                        <TableCell>
                                            {
                                                s.isHide ? <Button variant='outlined' sx={{ color: "#ffab00", borderColor: "#ffab00" }}
                                                    onClick={() => { setOpenConfirm(true); setCurrentPackage(s) }}>Hiện</Button> :
                                                    <Button variant='outlined' sx={{ color: "#ff3e1d", borderColor: "#ff3e1d" }}
                                                        onClick={() => { setOpenConfirm(true); setCurrentPackage(s) }}
                                                    >Ẩn</Button>
                                            }
                                            <PaymentUpdate paymentPackages={paymentPackages} paymentPackage={s} setStatus={setStatus} status={status} setPaymentPackages={setPaymetPackages} />
                                        </TableCell>
                                    </TableRow>
                                )
                            })
                        }
                    </TableBody>
                </Table>
            </TableContainer>
            <ConfirmDialog openConfirm={openConfirm}
                setOpenConfirm={setOpenConfirm}
                title="Đổi trạng thái"
                handleAction={changeStatus}
                content={`Bạn có chắc muốn ${currentPackage && currentPackage.isHide ? "hiện" : "ẩn"} gói thanh toán này không`} />
        </Paper>
    )
}

export default PaymentPackageManagement
